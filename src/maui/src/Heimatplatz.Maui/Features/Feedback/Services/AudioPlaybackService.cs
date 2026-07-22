using Microsoft.Extensions.Logging;
using Shiny;

namespace Heimatplatz.Maui.Features.Feedback.Services;

#if ANDROID

/// <summary>Wiedergabe via Android MediaPlayer (WAV/M4A von lokalem Pfad).</summary>
[Singleton]
public class AudioPlaybackService(ILogger<AudioPlaybackService> logger) : IAudioPlaybackService
{
    private Android.Media.MediaPlayer? _player;

    public bool IsSupported => true;
    public bool IsPlaying => _player?.IsPlaying == true;

    public event EventHandler? PlaybackEnded;

    public async Task<bool> PlayFileAsync(string filePath)
    {
        await StopAsync();

        try
        {
            var player = new Android.Media.MediaPlayer();
            player.Completion += (_, _) => OnEnded();
            await player.SetDataSourceAsync(filePath);
            player.Prepare();
            player.Start();
            _player = player;
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[AudioPlayback] Wiedergabe fehlgeschlagen: {Path}", filePath);
            OnEnded();
            return false;
        }
    }

    public Task StopAsync()
    {
        var player = _player;
        _player = null;
        if (player != null)
        {
            try
            {
                if (player.IsPlaying)
                    player.Stop();
                player.Release();
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "[AudioPlayback] Fehler beim Stoppen");
            }

            PlaybackEnded?.Invoke(this, EventArgs.Empty);
        }

        return Task.CompletedTask;
    }

    private void OnEnded()
    {
        _player?.Release();
        _player = null;
        PlaybackEnded?.Invoke(this, EventArgs.Empty);
    }
}

#elif IOS

using AVFoundation;
using Foundation;

/// <summary>Wiedergabe via AVAudioPlayer (WAV/M4A von lokalem Pfad).</summary>
[Singleton]
public class AudioPlaybackService(ILogger<AudioPlaybackService> logger) : IAudioPlaybackService
{
    private AVAudioPlayer? _player;

    public bool IsSupported => true;
    public bool IsPlaying => _player?.Playing == true;

    public event EventHandler? PlaybackEnded;

    public async Task<bool> PlayFileAsync(string filePath)
    {
        await StopAsync();

        try
        {
            // Lautsprecher statt Hoerer-Muschel; Fehler hier sind nicht fatal
            var session = AVAudioSession.SharedInstance();
            session.SetCategory(AVAudioSessionCategory.Playback);
            session.SetActive(true);

            var player = AVAudioPlayer.FromUrl(NSUrl.FromFilename(filePath), out var error);
            if (player == null || error != null)
            {
                logger.LogWarning("[AudioPlayback] AVAudioPlayer-Fehler: {Error}", error?.LocalizedDescription);
                return false;
            }

            player.FinishedPlaying += (_, _) => OnEnded();
            player.PrepareToPlay();
            player.Play();
            _player = player;
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[AudioPlayback] Wiedergabe fehlgeschlagen: {Path}", filePath);
            OnEnded();
            return false;
        }
    }

    public Task StopAsync()
    {
        var player = _player;
        _player = null;
        if (player != null)
        {
            try
            {
                player.Stop();
                player.Dispose();
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "[AudioPlayback] Fehler beim Stoppen");
            }

            PlaybackEnded?.Invoke(this, EventArgs.Empty);
        }

        return Task.CompletedTask;
    }

    private void OnEnded()
    {
        _player?.Dispose();
        _player = null;
        PlaybackEnded?.Invoke(this, EventArgs.Empty);
    }
}

#else

/// <summary>Stub fuer Windows/MacCatalyst: keine Sprachnachrichten-Wiedergabe.</summary>
[Singleton]
public class AudioPlaybackService(ILogger<AudioPlaybackService> logger) : IAudioPlaybackService
{
    public bool IsSupported => false;
    public bool IsPlaying => false;

#pragma warning disable CS0067 // Event wird auf dieser Plattform nie ausgeloest
    public event EventHandler? PlaybackEnded;
#pragma warning restore CS0067

    public Task<bool> PlayFileAsync(string filePath)
    {
        logger.LogInformation("[AudioPlayback] Nicht unterstuetzt auf dieser Plattform");
        return Task.FromResult(false);
    }

    public Task StopAsync() => Task.CompletedTask;
}

#endif
