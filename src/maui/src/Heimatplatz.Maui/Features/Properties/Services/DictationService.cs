using Microsoft.Extensions.Logging;
using Shiny;

namespace Heimatplatz.Maui.Features.Properties.Services;

#if ANDROID || IOS

using System.Globalization;
using Heimatplatz.Maui.Localization.Properties;
using Shiny.Speech;

/// <summary>
/// Diktat via Shiny.Speech (plattform-native Spracherkennung).
/// Kontinuierlicher Modus: Ein Watchdog-Loop ist die EINZIGE Stelle, die die
/// Plattform-Erkennung (neu) startet - er ueberwacht IsListening und startet
/// die Erkennung neu, sobald sie ausgelaufen ist (Sprechpause, Session-Ende,
/// verzoegertes Stop). Damit kann der gemeldete Zustand nie vom echten
/// Erkennungszustand abdriften (Symbol zeigt Aufnahme, aber nichts laeuft bzw.
/// Aufnahme laeuft nach Stopp weiter).
/// </summary>
[Singleton]
public class DictationService(
    ISpeechToTextService speech,
    DictationStringsLocalized loc,
    ILogger<DictationService> logger
) : IDictationService
{
    private static readonly TimeSpan WatchdogInterval = TimeSpan.FromMilliseconds(600);
    private const int MaxStartFailures = 3;
    private const int MaxConsecutiveErrors = 5;

    private CancellationTokenSource? _watchdogCts;
    private int _consecutiveErrors;
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
        _consecutiveErrors = 0;

        if (!await TryStartRecognitionAsync())
        {
            GiveUp(loc.StartFailed);
            return;
        }

        StartWatchdog();
    }

    public async Task StopAsync()
    {
        _shouldListen = false;
        StopWatchdog();

        // Immer stoppen (auch wenn IsListening false wirkt): eine gerade erst
        // angelaufene Session wuerde sonst nach dem Stopp-Tap weiterlauschen
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

    /// <summary>
    /// Startet die Plattform-Erkennung, wenn sie nicht schon laeuft.
    /// Liefert false nur bei einem echten Startfehler.
    /// </summary>
    private async Task<bool> TryStartRecognitionAsync()
    {
        try
        {
            if (speech.IsListening)
                return true;

            await speech.Start(new SpeechRecognitionOptions
            {
                Culture = CultureInfo.GetCultureInfo("de-AT"),
                SilenceTimeout = TimeSpan.FromSeconds(5),
                PreferOnDevice = false
            });

            // Stopp kam waehrend des Starts dazwischen - Erkennung sofort wieder beenden,
            // sonst laeuft das Mikrofon weiter obwohl die UI "gestoppt" zeigt
            if (!_shouldListen)
            {
                try { await speech.Stop(); }
                catch (Exception ex) { logger.LogWarning(ex, "[Dictation] Stop nach spaetem Start fehlgeschlagen"); }
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Dictation] Start der Erkennung fehlgeschlagen");
            return false;
        }
    }

    #region Watchdog

    private void StartWatchdog()
    {
        StopWatchdog();
        var cts = new CancellationTokenSource();
        _watchdogCts = cts;

        // Auf dem Main-Thread laufen lassen: Android SpeechRecognizer verlangt
        // Start/Stop vom UI-Thread (die awaits bleiben durch den MAUI-Kontext dort)
        _ = MainThread.InvokeOnMainThreadAsync(() => RunWatchdogAsync(cts.Token));
    }

    private void StopWatchdog()
    {
        _watchdogCts?.Cancel();
        _watchdogCts?.Dispose();
        _watchdogCts = null;
    }

    private async Task RunWatchdogAsync(CancellationToken ct)
    {
        var startFailures = 0;

        try
        {
            while (_shouldListen && !ct.IsCancellationRequested)
            {
                await Task.Delay(WatchdogInterval, ct);

                if (!_shouldListen || ct.IsCancellationRequested)
                    return;

                if (speech.IsListening)
                {
                    startFailures = 0;
                    continue;
                }

                if (await TryStartRecognitionAsync())
                {
                    startFailures = 0;
                }
                else if (++startFailures >= MaxStartFailures)
                {
                    GiveUp(loc.Interrupted);
                    return;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Diktat wurde beendet
        }
    }

    /// <summary>Beendet das Diktat wegen wiederholter Fehler und informiert die UI.</summary>
    private void GiveUp(string message)
    {
        _shouldListen = false;
        StopWatchdog();
        Failed?.Invoke(this, message);
        Stopped?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    private void OnResultReceived(object? sender, SpeechRecognitionResult result)
    {
        _consecutiveErrors = 0;

        if (result.IsFinal)
        {
            if (!string.IsNullOrWhiteSpace(result.Text))
                FinalResult?.Invoke(this, result.Text.Trim());

            // Neustart nach einem finalen Segment uebernimmt der Watchdog
        }
        else
        {
            PartialResult?.Invoke(this, result.Text);
        }
    }

    private void OnError(object? sender, SpeechRecognitionError error)
    {
        logger.LogWarning("[Dictation] Fehler: {Message}", error.Message);

        // Transiente Fehler (z.B. keine Sprache erkannt) ueberlebt der Watchdog durch
        // Neustart; haeufen sie sich ohne ein einziges Ergebnis dazwischen, wird sauber
        // beendet statt endlos im Hintergrund weiterzulauschen
        if (_shouldListen && ++_consecutiveErrors >= MaxConsecutiveErrors)
        {
            MainThread.BeginInvokeOnMainThread(() =>
                GiveUp(loc.NoSpeechDetected));
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
