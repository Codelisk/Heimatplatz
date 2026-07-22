using Microsoft.Extensions.Logging;
using Shiny;

namespace Heimatplatz.Maui.Features.Feedback.Services;

// Bewusst native Aufnahme statt Shiny.Speech: das veroeffentlichte Paket
// (3.0.0-beta-0014) enthaelt die IAudioSource-API noch nicht.

#if ANDROID

using Android.Media;
using MauiPermissions = Microsoft.Maui.ApplicationModel.Permissions;

/// <summary>
/// Sprachaufnahme via Android AudioRecord: rohes PCM (16 kHz, 16 bit, mono) wird
/// in eine WAV-Datei im App-Cache kopiert (Header-Fixup beim Stopp). Die Dauer
/// ergibt sich aus den geschriebenen Bytes.
/// </summary>
[Singleton]
public class VoiceRecorderService(ILogger<VoiceRecorderService> logger) : IVoiceRecorderService
{
    private const string CacheSubfolder = "feedback-audio";

    private AudioRecord? _recorder;
    private FileStream? _fileStream;
    private Task? _copyTask;
    private string? _filePath;
    private long _bytesWritten;
    private volatile bool _recording;

    public bool IsSupported => true;

    public bool IsRecording => _recording;

    public TimeSpan Elapsed => TimeSpan.FromSeconds((double)Interlocked.Read(ref _bytesWritten) / WavFile.BytesPerSecond);

    public async Task<bool> RequestAccessAsync()
    {
        var status = await MauiPermissions.RequestAsync<MauiPermissions.Microphone>();
        logger.LogInformation("[VoiceRecorder] Mikrofon-Berechtigung: {Status}", status);
        return status == PermissionStatus.Granted;
    }

    public Task StartAsync()
    {
        if (_recording)
            return Task.CompletedTask;

        var cacheDir = Path.Combine(FileSystem.CacheDirectory, CacheSubfolder);
        Directory.CreateDirectory(cacheDir);
        _filePath = Path.Combine(cacheDir, $"{Guid.NewGuid():N}.wav");

        var minBufferSize = AudioRecord.GetMinBufferSize(WavFile.SampleRate, ChannelIn.Mono, Encoding.Pcm16bit);
        var recorder = new AudioRecord(
            AudioSource.Mic, WavFile.SampleRate, ChannelIn.Mono, Encoding.Pcm16bit,
            Math.Max(minBufferSize, WavFile.BytesPerSecond / 2));

        if (recorder.State != State.Initialized)
        {
            recorder.Release();
            throw new InvalidOperationException("AudioRecord konnte nicht initialisiert werden.");
        }

        _fileStream = File.Create(_filePath);
        WavFile.WriteHeader(_fileStream, 0);
        Interlocked.Exchange(ref _bytesWritten, 0);

        recorder.StartRecording();
        _recorder = recorder;
        _recording = true;
        _copyTask = Task.Run(CopyLoop);

        return Task.CompletedTask;
    }

    public async Task<VoiceRecording?> StopAsync()
    {
        if (!_recording || _recorder == null || _fileStream == null || _filePath == null)
            return null;

        _recording = false;

        try
        {
            // Stop() vor dem Warten auf den Copy-Loop: ein blockierendes Read()
            // kehrt erst nach dem Stopp der Aufnahme zurueck
            _recorder.Stop();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[VoiceRecorder] Fehler beim Stoppen der Aufnahme");
        }

        if (_copyTask != null)
            await _copyTask;

        _recorder.Release();
        _recorder = null;
        _copyTask = null;

        var dataLength = Interlocked.Read(ref _bytesWritten);
        WavFile.FixupHeader(_fileStream, dataLength);
        await _fileStream.DisposeAsync();
        _fileStream = null;

        var filePath = _filePath;
        _filePath = null;

        if (dataLength <= 0)
        {
            TryDelete(filePath);
            return null;
        }

        var duration = (double)dataLength / WavFile.BytesPerSecond;
        logger.LogInformation("[VoiceRecorder] Aufnahme fertig: {Seconds:F1}s, {Bytes} Bytes", duration, dataLength);
        return new VoiceRecording(filePath, duration, dataLength + WavFile.HeaderLength);
    }

    public async Task CancelAsync()
    {
        var recording = await StopAsync();
        TryDelete(recording?.FilePath);
    }

    private void CopyLoop()
    {
        var buffer = new byte[3200]; // 100 ms Audio
        try
        {
            while (_recording && _recorder != null && _fileStream != null)
            {
                var read = _recorder.Read(buffer, 0, buffer.Length);
                if (read <= 0)
                    break;

                _fileStream.Write(buffer, 0, read);
                Interlocked.Add(ref _bytesWritten, read);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[VoiceRecorder] Copy-Loop beendet");
        }
    }

    private static void TryDelete(string? path)
    {
        try
        {
            if (path != null && File.Exists(path))
                File.Delete(path);
        }
        catch (IOException)
        {
            // Cache-Datei raeumt sonst das OS auf
        }
    }
}

#elif IOS

using AVFoundation;
using Foundation;
using MauiPermissions = Microsoft.Maui.ApplicationModel.Permissions;

/// <summary>
/// Sprachaufnahme via AVAudioRecorder: schreibt direkt eine WAV-Datei
/// (LinearPCM, 16 kHz, 16 bit, mono) in den App-Cache.
/// </summary>
[Singleton]
public class VoiceRecorderService(ILogger<VoiceRecorderService> logger) : IVoiceRecorderService
{
    private const string CacheSubfolder = "feedback-audio";

    private AVAudioRecorder? _recorder;
    private string? _filePath;

    public bool IsSupported => true;

    public bool IsRecording => _recorder?.Recording == true;

    public TimeSpan Elapsed => TimeSpan.FromSeconds(_recorder?.CurrentTime ?? 0);

    public async Task<bool> RequestAccessAsync()
    {
        var status = await MauiPermissions.RequestAsync<MauiPermissions.Microphone>();
        logger.LogInformation("[VoiceRecorder] Mikrofon-Berechtigung: {Status}", status);
        return status == PermissionStatus.Granted;
    }

    public Task StartAsync()
    {
        if (IsRecording)
            return Task.CompletedTask;

        var session = AVAudioSession.SharedInstance();
        session.SetCategory(AVAudioSessionCategory.PlayAndRecord);
        session.SetActive(true);

        var cacheDir = Path.Combine(FileSystem.CacheDirectory, CacheSubfolder);
        Directory.CreateDirectory(cacheDir);
        _filePath = Path.Combine(cacheDir, $"{Guid.NewGuid():N}.wav");

        var settings = new AudioSettings
        {
            SampleRate = WavFile.SampleRate,
            Format = AudioToolbox.AudioFormatType.LinearPCM,
            NumberChannels = WavFile.Channels,
            LinearPcmBitDepth = WavFile.BitsPerSample,
            LinearPcmBigEndian = false,
            LinearPcmFloat = false
        };

        var recorder = AVAudioRecorder.Create(NSUrl.FromFilename(_filePath), settings, out var error);
        if (recorder == null || error != null)
            throw new InvalidOperationException($"AVAudioRecorder-Fehler: {error?.LocalizedDescription}");

        recorder.PrepareToRecord();
        recorder.Record();
        _recorder = recorder;

        return Task.CompletedTask;
    }

    public Task<VoiceRecording?> StopAsync()
    {
        var recorder = _recorder;
        var filePath = _filePath;
        _recorder = null;
        _filePath = null;

        if (recorder == null || filePath == null)
            return Task.FromResult<VoiceRecording?>(null);

        var duration = recorder.CurrentTime;
        recorder.Stop();
        recorder.Dispose();

        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists || fileInfo.Length <= WavFile.HeaderLength || duration <= 0)
        {
            TryDelete(filePath);
            return Task.FromResult<VoiceRecording?>(null);
        }

        logger.LogInformation("[VoiceRecorder] Aufnahme fertig: {Seconds:F1}s, {Bytes} Bytes", duration, fileInfo.Length);
        return Task.FromResult<VoiceRecording?>(new VoiceRecording(filePath, duration, fileInfo.Length));
    }

    public async Task CancelAsync()
    {
        var recording = await StopAsync();
        TryDelete(recording?.FilePath);
    }

    private static void TryDelete(string? path)
    {
        try
        {
            if (path != null && File.Exists(path))
                File.Delete(path);
        }
        catch (IOException)
        {
            // Cache-Datei raeumt sonst das OS auf
        }
    }
}

#else

/// <summary>
/// Stub fuer Plattformen ohne Mikrofon-Aufnahme (Windows/MacCatalyst):
/// Sprachnachrichten sind dort nicht verfuegbar, die Aufnahme-UI bleibt ausgeblendet.
/// </summary>
[Singleton]
public class VoiceRecorderService(ILogger<VoiceRecorderService> logger) : IVoiceRecorderService
{
    public bool IsSupported => false;
    public bool IsRecording => false;
    public TimeSpan Elapsed => TimeSpan.Zero;

    public Task<bool> RequestAccessAsync()
    {
        logger.LogInformation("[VoiceRecorder] Nicht unterstuetzt auf dieser Plattform");
        return Task.FromResult(false);
    }

    public Task StartAsync() => Task.CompletedTask;

    public Task<VoiceRecording?> StopAsync() => Task.FromResult<VoiceRecording?>(null);

    public Task CancelAsync() => Task.CompletedTask;
}

#endif
