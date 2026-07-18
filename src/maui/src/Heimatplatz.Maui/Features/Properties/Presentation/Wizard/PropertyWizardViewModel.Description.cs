using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Maui.ApiClient.Generated;
using Microsoft.Extensions.Logging;

namespace Heimatplatz.Maui.Features.Properties.Presentation.Wizard;

/// <summary>
/// Schritt 4: Beschreibung. Der Nutzer waehlt zwischen zwei Wegen:
/// selbst schreiben ODER aus Stichwoertern/Diktat erstellen lassen.
/// Die Generierung laeuft als Server-Hintergrund-Job (dauert einige Minuten) -
/// der Wizard pollt den Status und der Nutzer kann sofort weiterarbeiten,
/// den Wizard sogar schliessen: der fertige Text landet im Entwurf.
/// </summary>
public partial class PropertyWizardViewModel
{
    private static readonly TimeSpan DescriptionPollInterval = TimeSpan.FromSeconds(5);

    private CancellationTokenSource? _descriptionPollCts;

    #region Modus-Wahl

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsManualMode))]
    [NotifyPropertyChangedFor(nameof(IsGenerateMode))]
    public partial DraftDescriptionMode DescriptionMode { get; set; }

    public bool IsManualMode => DescriptionMode == DraftDescriptionMode.Manual;
    public bool IsGenerateMode => DescriptionMode == DraftDescriptionMode.Generate;

    [RelayCommand]
    private void ChooseManualMode() => DescriptionMode = DraftDescriptionMode.Manual;

    [RelayCommand]
    private void ChooseGenerateMode() => DescriptionMode = DraftDescriptionMode.Generate;

    #endregion

    #region Texte

    /// <summary>Finaler Beschreibungstext (manuell oder uebernommener generierter Text)</summary>
    [ObservableProperty]
    public partial string Beschreibung { get; set; }

    /// <summary>Stichwoerter/Diktat als Basis fuer die Generierung</summary>
    [ObservableProperty]
    public partial string DescriptionKeywords { get; set; }

    #endregion

    #region Generierungs-Status

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsGenerationRunning))]
    [NotifyPropertyChangedFor(nameof(IsGenerationFinished))]
    [NotifyPropertyChangedFor(nameof(CanRequestGeneration))]
    [NotifyPropertyChangedFor(nameof(RequestGenerationButtonText))]
    public partial DraftDescriptionStatus GenerationStatus { get; set; }

    public bool IsGenerationRunning =>
        GenerationStatus is DraftDescriptionStatus.Queued or DraftDescriptionStatus.InProgress;

    public bool IsGenerationFinished => GenerationStatus == DraftDescriptionStatus.Finished;

    public bool CanRequestGeneration => !IsGenerationRunning;

    /// <summary>Button-Text: nach einer fertigen Generierung wird "neu erstellen" angeboten</summary>
    public string RequestGenerationButtonText => IsGenerationFinished
        ? Loc.RequestGenerationAgainButton
        : Loc.RequestGenerationButton;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasGenerationFailed))]
    public partial string? GenerationFailedMessage { get; set; }

    public bool HasGenerationFailed => !string.IsNullOrEmpty(GenerationFailedMessage);

    /// <summary>Der zuletzt vom Server generierte Text (Basis fuer "Text uebernehmen")</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowApplyGeneratedButton))]
    public partial string? GeneratedDescription { get; set; }

    /// <summary>
    /// "Text uebernehmen" nur anbieten, wenn der generierte Text NICHT schon im Feld steht
    /// (z.B. weil der Nutzer waehrend der Generierung selbst etwas geschrieben hat).
    /// </summary>
    public bool ShowApplyGeneratedButton =>
        IsGenerationFinished
        && !string.IsNullOrWhiteSpace(GeneratedDescription)
        && Beschreibung != GeneratedDescription;

    partial void OnBeschreibungChanged(string value) => OnPropertyChanged(nameof(ShowApplyGeneratedButton));

    #endregion

    private void InitializeDescriptionStep()
    {
        Beschreibung = string.Empty;
        DescriptionKeywords = string.Empty;
        DictationLivePartialInit();
    }

    #region Diktat (Stichwoerter einsprechen)

    public bool DictationSupported => _dictation.IsSupported;
    public bool DictationUnsupported => !_dictation.IsSupported;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMicIdle))]
    [NotifyPropertyChangedFor(nameof(MicHintText))]
    public partial bool IsListening { get; set; }

    /// <summary>Steuert das Mikrofon-Icon (Gegenstueck zum Stopp-Quadrat bei Aufnahme)</summary>
    public bool IsMicIdle => !IsListening;

    public string MicHintText => IsListening
        ? Loc.MicHintListening
        : Loc.MicHintIdle;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLivePartial))]
    public partial string LivePartial { get; set; }

    public bool HasLivePartial => !string.IsNullOrEmpty(LivePartial);

    private void DictationLivePartialInit() => LivePartial = string.Empty;

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

        // Der DictationService ist ein Singleton - beim (Wieder-)Erscheinen der Seite
        // den echten Zustand uebernehmen, damit das Icon nie falsch startet
        IsListening = _dictation.IsListening;
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
            if (IsListening || _dictation.IsListening)
            {
                // Sofort optisch stoppen - das Stopped-Event bestaetigt nur noch
                IsListening = false;
                await _dictation.StopAsync();
                return;
            }

            if (!_dictation.IsSupported)
            {
                ErrorMessage = Loc.DictationNotAvailable;
                return;
            }

            if (!await _dictation.RequestPermissionAsync())
            {
                ErrorMessage = Loc.MicPermissionDenied;
                return;
            }

            ErrorMessage = null;
            await _dictation.StartAsync();

            // Zustand vom Service uebernehmen statt blind auf true zu setzen -
            // ein fehlgeschlagener Start hat IsListening dort schon zurueckgesetzt
            IsListening = _dictation.IsListening;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PropertyWizard] Fehler beim Diktat");
            ErrorMessage = Loc.DictationErrorFormat(ex.Message);
            IsListening = false;
        }
    }

    private void OnDictationPartial(object? sender, string text) =>
        MainThread.BeginInvokeOnMainThread(() => LivePartial = text);

    private void OnDictationFinal(object? sender, string text) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            DescriptionKeywords = string.IsNullOrWhiteSpace(DescriptionKeywords)
                ? text
                : $"{DescriptionKeywords.TrimEnd()} {text}";
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

    #region Generierung anfordern + Polling

    /// <summary>
    /// Fordert den Beschreibungs-Job an: Entwurf speichern (der Job liest Eckdaten und
    /// Foto-URLs aus dem Entwurf), dann Job einreihen und Status-Polling starten.
    /// </summary>
    [RelayCommand]
    private async Task RequestGenerationAsync()
    {
        ErrorMessage = null;

        if (IsListening)
            await _dictation.StopAsync();

        if (string.IsNullOrWhiteSpace(DescriptionKeywords) || DescriptionKeywords.Trim().Length < 3)
        {
            ErrorMessage = Loc.ValidationKeywordsMissing;
            return;
        }

        if (IsGenerationRunning)
            return;

        IsBusy = true;
        BusyMessage = Loc.BusyRequestingGeneration;

        try
        {
            // Fotos zuerst fertig hochladen - der Job bezieht sie ueber den Entwurf ein
            await EnsureMediaUploadedAsync();

            MarkDraftDirty();
            if (!await SaveDraftAsync() || _serverDraftId is not { } draftId)
            {
                ErrorMessage = Loc.DraftSaveError;
                return;
            }

            var (_, response) = await _mediator.Request(new GenerateDraftDescriptionHttpRequest
            {
                Body = new GenerateDraftDescriptionRequest
                {
                    Id = draftId,
                    Keywords = DescriptionKeywords.Trim()
                }
            });

            if (response == null)
                throw new InvalidOperationException("Keine Antwort vom Server.");

            GenerationFailedMessage = null;
            GeneratedDescription = null;
            GenerationStatus = response.Status;
            StartDescriptionPolling(draftId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PropertyWizard] Beschreibungs-Job konnte nicht gestartet werden");
            ErrorMessage = Loc.GenerationRequestFailed;
        }
        finally
        {
            IsBusy = false;
            BusyMessage = null;
        }
    }

    /// <summary>Uebernimmt den generierten Text ins Beschreibungs-Feld (ueberschreibt bewusst).</summary>
    [RelayCommand]
    private async Task ApplyGeneratedDescriptionAsync()
    {
        if (string.IsNullOrWhiteSpace(GeneratedDescription))
            return;

        Beschreibung = GeneratedDescription;
        MarkDraftDirty();
        await SaveDraftAsync();
    }

    /// <summary>Startet das Status-Polling (idempotent - ein laufender Loop wird ersetzt).</summary>
    private void StartDescriptionPolling(Guid draftId)
    {
        StopDescriptionPolling();
        _descriptionPollCts = new CancellationTokenSource();
        _ = PollDescriptionAsync(draftId, _descriptionPollCts.Token);
    }

    private void StopDescriptionPolling()
    {
        _descriptionPollCts?.Cancel();
        _descriptionPollCts?.Dispose();
        _descriptionPollCts = null;
    }

    private async Task PollDescriptionAsync(Guid draftId, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(DescriptionPollInterval, ct);

                GetDraftDescriptionResponse? status = null;
                try
                {
                    (_, status) = await _mediator.Request(new GetDraftDescriptionHttpRequest { Id = draftId }, ct);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // Transiente Netzfehler ueberstehen - der Job laeuft serverseitig weiter
                    _logger.LogWarning(ex, "[PropertyWizard] Beschreibungs-Polling fehlgeschlagen, naechster Versuch folgt");
                }

                if (status == null)
                    continue;

                if (status.Status == DraftDescriptionStatus.Finished)
                {
                    MainThread.BeginInvokeOnMainThread(() => OnGenerationFinished(status.GeneratedText));
                    return;
                }

                if (status.Status == DraftDescriptionStatus.Failed)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        GenerationStatus = DraftDescriptionStatus.Failed;
                        GenerationFailedMessage = Loc.GenerationFailed;
                    });
                    return;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Polling bewusst gestoppt (Abbrechen/Verwerfen/Publish)
        }
    }

    private void OnGenerationFinished(string? generatedText)
    {
        GenerationStatus = DraftDescriptionStatus.Finished;
        GenerationFailedMessage = null;
        GeneratedDescription = generatedText;

        // Nur ein leeres Feld automatisch befuellen - eigener Text gewinnt immer,
        // die Uebernahme bleibt dann per "Text uebernehmen" moeglich
        if (string.IsNullOrWhiteSpace(Beschreibung) && !string.IsNullOrWhiteSpace(generatedText))
        {
            Beschreibung = generatedText;
            MarkDraftDirty();
            _ = SaveDraftAsync();
        }

        RefreshEditorState();
    }

    /// <summary>
    /// Stellt den Generierungs-Zustand aus einem geladenen Entwurf wieder her
    /// und pollt einen offenen Job weiter.
    /// </summary>
    private void RestoreDescriptionState(GetPropertyDraftResponse response)
    {
        GenerationStatus = response.DescriptionStatus;
        GeneratedDescription = response.GeneratedDescription;

        if (!string.IsNullOrWhiteSpace(response.SubmittedDescriptionKeywords)
            && string.IsNullOrWhiteSpace(DescriptionKeywords))
            DescriptionKeywords = response.SubmittedDescriptionKeywords;

        switch (response.DescriptionStatus)
        {
            case DraftDescriptionStatus.Queued:
            case DraftDescriptionStatus.InProgress:
                StartDescriptionPolling(response.Id);
                break;

            case DraftDescriptionStatus.Finished
                when string.IsNullOrWhiteSpace(Beschreibung) && !string.IsNullOrWhiteSpace(response.GeneratedDescription):
                Beschreibung = response.GeneratedDescription;
                break;

            case DraftDescriptionStatus.Failed:
                GenerationFailedMessage = Loc.GenerationFailed;
                break;
        }
    }

    #endregion

    private bool ValidateDescriptionText() =>
        !string.IsNullOrWhiteSpace(Beschreibung) && Beschreibung.Trim().Length >= 50;
}
