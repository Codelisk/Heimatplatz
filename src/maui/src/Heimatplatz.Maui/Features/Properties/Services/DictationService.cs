using Microsoft.Extensions.Logging;
using Shiny;

namespace Heimatplatz.Maui.Features.Properties.Services;

#if ANDROID || IOS

using System.Globalization;
using Shiny.Speech;

/// <summary>
/// Diktat via Shiny.Speech (plattform-native Spracherkennung).
/// Kontinuierlicher Modus: Nach einem finalen Segment (Sprechpause) wird die
/// Erkennung automatisch neu gestartet, solange der Nutzer das Diktat nicht beendet.
/// </summary>
[Singleton]
public class DictationService(
    ISpeechToTextService speech,
    ILogger<DictationService> logger
) : IDictationService
{
    private bool _shouldListen;
    private bool _subscribed;

    public bool IsSupported => true;
    public bool IsListening => _shouldListen;

    public event EventHandler<string>? PartialResult;
    public event EventHandler<string>? FinalResult;
    public event EventHandler<string>? Failed;
    public event EventHandler? Stopped;

    public async Task<bool> RequestPermissionAsync()
    {
        var access = await speech.RequestAccess();
        logger.LogInformation("[Dictation] AccessState: {Access}", access);
        return access == AccessState.Available;
    }

    public async Task StartAsync()
    {
        if (_shouldListen)
            return;

        if (!_subscribed)
        {
            speech.ResultReceived += OnResultReceived;
            speech.Error += OnError;
            _subscribed = true;
        }

        _shouldListen = true;
        await StartCoreAsync();
    }

    public async Task StopAsync()
    {
        _shouldListen = false;

        try
        {
            await speech.Stop();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Dictation] Fehler beim Stoppen");
        }

        Stopped?.Invoke(this, EventArgs.Empty);
    }

    private async Task StartCoreAsync()
    {
        try
        {
            if (speech.IsListening)
                return;

            await speech.Start(new SpeechRecognitionOptions
            {
                Culture = CultureInfo.GetCultureInfo("de-AT"),
                SilenceTimeout = TimeSpan.FromSeconds(5),
                PreferOnDevice = false
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Dictation] Start fehlgeschlagen");
            _shouldListen = false;
            Failed?.Invoke(this, "Diktat konnte nicht gestartet werden.");
            Stopped?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnResultReceived(object? sender, SpeechRecognitionResult result)
    {
        if (result.IsFinal)
        {
            if (!string.IsNullOrWhiteSpace(result.Text))
                FinalResult?.Invoke(this, result.Text.Trim());

            // Plattform-Erkennung laeuft nach einem finalen Segment oft aus:
            // bei aktivem Diktat automatisch neu starten
            if (_shouldListen)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Task.Delay(300);
                    if (_shouldListen && !speech.IsListening)
                        await StartCoreAsync();
                });
            }
        }
        else
        {
            PartialResult?.Invoke(this, result.Text);
        }
    }

    private void OnError(object? sender, SpeechRecognitionError error)
    {
        logger.LogWarning("[Dictation] Fehler: {Message}", error.Message);

        // Transiente Fehler (z.B. keine Sprache erkannt) bei aktivem Diktat: neu starten
        if (_shouldListen)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(500);
                if (_shouldListen && !speech.IsListening)
                    await StartCoreAsync();
            });
        }
    }
}

#else

/// <summary>
/// Stub fuer Plattformen ohne Shiny.Speech (Windows/MacCatalyst):
/// Diktat ist dort nicht verfuegbar, der manuelle Weg ist der Standard.
/// </summary>
[Singleton]
public class DictationService(ILogger<DictationService> logger) : IDictationService
{
    public bool IsSupported => false;
    public bool IsListening => false;

#pragma warning disable CS0067 // Events werden auf dieser Plattform nie ausgeloest
    public event EventHandler<string>? PartialResult;
    public event EventHandler<string>? FinalResult;
    public event EventHandler<string>? Failed;
    public event EventHandler? Stopped;
#pragma warning restore CS0067

    public Task<bool> RequestPermissionAsync()
    {
        logger.LogInformation("[Dictation] Nicht unterstuetzt auf dieser Plattform");
        return Task.FromResult(false);
    }

    public Task StartAsync() => Task.CompletedTask;

    public Task StopAsync() => Task.CompletedTask;
}

#endif
