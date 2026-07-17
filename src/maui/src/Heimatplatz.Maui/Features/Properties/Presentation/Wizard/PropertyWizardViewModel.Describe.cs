using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Heimatplatz.Maui.Features.Properties.Presentation.Wizard;

/// <summary>
/// Schritt 2: Beschreiben - Diktat (Shiny.Speech, nur Android/iOS) und/oder Freitext.
/// "Weiter" mit Text startet die KI-Analyse im Hintergrund (der Nutzer fuellt derweil
/// Lage &amp; Preis aus); "Ohne KI fortfahren" ueberspringt die Analyse bewusst.
/// Diktat-Wiring unveraendert aus dem frueheren AiAddPropertyViewModel uebernommen.
/// </summary>
public partial class PropertyWizardViewModel
{
    public bool DictationSupported => _dictation.IsSupported;
    public bool DictationUnsupported => !_dictation.IsSupported;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MicButtonText))]
    [NotifyPropertyChangedFor(nameof(MicHintText))]
    public partial bool IsListening { get; set; }

    public string MicButtonText => IsListening ? "◼" : "🎤";

    public string MicHintText => IsListening
        ? "Aufnahme läuft – tippen zum Stoppen"
        : "Tippen und Immobilie beschreiben";

    [ObservableProperty]
    public partial string DictatedText { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLivePartial))]
    public partial string LivePartial { get; set; }

    public bool HasLivePartial => !string.IsNullOrEmpty(LivePartial);

    /// <summary>Nutzer hat die KI-Analyse bewusst uebersprungen (persistiert im Entwurf)</summary>
    [ObservableProperty]
    public partial bool AiSkipped { get; set; }

    private void InitializeDescribeStep()
    {
        DictatedText = string.Empty;
        LivePartial = string.Empty;
    }

    #region Diktat

    private void SubscribeDictation()
    {
        // Dictation-Events pro Sichtbarkeit koppeln: der DictationService ist ein
        // Singleton, das VM wird aber pro Navigation neu erstellt - Konstruktor-Abos
        // wuerden sich dort ansammeln (Leak + doppelte Callbacks)
        UnsubscribeDictation();
        _dictation.PartialResult += OnDictationPartial;
        _dictation.FinalResult += OnDictationFinal;
        _dictation.Failed += OnDictationFailed;
        _dictation.Stopped += OnDictationStopped;
    }

    private void UnsubscribeDictation()
    {
        _dictation.PartialResult -= OnDictationPartial;
        _dictation.FinalResult -= OnDictationFinal;
        _dictation.Failed -= OnDictationFailed;
        _dictation.Stopped -= OnDictationStopped;
    }

    [RelayCommand]
    private async Task ToggleDictationAsync()
    {
        try
        {
            if (IsListening)
            {
                await _dictation.StopAsync();
                return;
            }

            if (!_dictation.IsSupported)
            {
                ErrorMessage = "Diktat ist auf diesem Gerät nicht verfügbar.";
                return;
            }

            if (!await _dictation.RequestPermissionAsync())
            {
                ErrorMessage = "Mikrofon-Berechtigung wurde nicht erteilt.";
                return;
            }

            ErrorMessage = null;
            await _dictation.StartAsync();
            IsListening = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PropertyWizard] Fehler beim Diktat");
            ErrorMessage = $"Fehler beim Diktat: {ex.Message}";
            IsListening = false;
        }
    }

    private void OnDictationPartial(object? sender, string text) =>
        MainThread.BeginInvokeOnMainThread(() => LivePartial = text);

    private void OnDictationFinal(object? sender, string text) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            DictatedText = string.IsNullOrWhiteSpace(DictatedText)
                ? text
                : $"{DictatedText.TrimEnd()} {text}";
            LivePartial = string.Empty;
        });

    private void OnDictationFailed(object? sender, string message) =>
        MainThread.BeginInvokeOnMainThread(() => ErrorMessage = message);

    private void OnDictationStopped(object? sender, EventArgs e) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsListening = false;
            LivePartial = string.Empty;
        });

    #endregion

    #region Weiter mit/ohne KI

    /// <summary>Bewusst ohne KI weiter - Eckdaten werden manuell erfasst.</summary>
    [RelayCommand]
    private async Task SkipAiAsync()
    {
        ErrorMessage = null;
        if (!await AdvanceFromDescribeAsync(skipAi: true))
            return;

        await GoToStepAsync(CurrentStep + 1);
    }

    /// <summary>
    /// Verlaesst den Beschreiben-Schritt: stoppt das Diktat und startet - sofern Text
    /// vorhanden und nicht uebersprungen - die KI-Analyse im Hintergrund.
    /// Liefert false, wenn der Wechsel (noch) nicht moeglich ist.
    /// </summary>
    private async Task<bool> AdvanceFromDescribeAsync(bool skipAi)
    {
        if (IsListening)
            await _dictation.StopAsync();

        if (skipAi)
        {
            AiSkipped = true;
            _runner.Cancel();
            return true;
        }

        AiSkipped = false;

        // Ohne Text keine Analyse (die KI-Extraktion arbeitet rein textbasiert) -
        // der Schritt darf trotzdem verlassen werden, Eckdaten dann manuell
        if (string.IsNullOrWhiteSpace(DictatedText))
        {
            AiSkipped = true;
            return true;
        }

        // Analyse laeuft bereits fuer den aktuellen Stand? Nicht neu starten.
        if (_runner.State == Services.ListingAnalysisRunState.Running && _runner.AnalysisId != null)
            return true;

        // Ergebnis wurde schon uebernommen (z.B. nach Entwurfs-Resume): keine erneute
        // Analyse - die Nur-in-leere-Felder-Regel wuerde neue Ergebnisse ohnehin verwerfen.
        if (AnalysisApplied)
            return true;

        try
        {
            IsBusy = true;
            BusyMessage = "Medien werden hochgeladen…";
            await EnsureMediaUploadedAsync();

            BusyMessage = "KI-Analyse wird gestartet…";
            var analysisId = await _runner.StartAsync(UploadedImageUrls, UploadedVideoUrls, DictatedText);
            if (analysisId == null)
            {
                // Startfehler nicht blockieren: Schritt 4 zeigt den manuellen Fallback
                _logger.LogWarning("[PropertyWizard] KI-Analyse konnte nicht gestartet werden - manueller Fallback");
            }

            AnalysisApplied = false;
            return true;
        }
        finally
        {
            IsBusy = false;
            BusyMessage = null;
        }
    }

    #endregion
}
