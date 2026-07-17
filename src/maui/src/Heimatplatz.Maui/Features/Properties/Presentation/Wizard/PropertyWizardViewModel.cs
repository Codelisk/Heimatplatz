using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Maui.Features.Auth;
using Heimatplatz.Maui.Features.Properties.Models;
using Heimatplatz.Maui.Features.Properties.Services;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Maui.Features.Properties.Presentation.Wizard;

/// <summary>
/// Einheitlicher Wizard fuer die Inseratserstellung (ersetzt AddProperty + AiAddProperty):
///   Schritt 1: Fotos &amp; Videos (Upload startet im Hintergrund)
///   Schritt 2: Beschreiben (Diktat/Text, ueberspringbar) - startet die KI-Analyse
///   Schritt 3: Lage &amp; Preis (Felder, die die KI nie liefert - laeuft parallel zur Analyse)
///   Schritt 4: Eckdaten pruefen (KI-Vorschlaege nur in leere Felder)
///   Schritt 5: Vorschau &amp; Veroeffentlichen
/// Jeder Schrittwechsel speichert den Zustand als Server-Entwurf; Abbruch bietet
/// Speichern/Verwerfen an, Fortsetzen laeuft ueber den DraftId-Navigationsparameter.
/// Aufgeteilt in partial-Dateien pro Schritt (.Media/.Describe/.LocationPrice/.Details/.Preview/.Draft).
/// </summary>
[ShellMap<PropertyWizardPage>("PropertyWizard")]
public partial class PropertyWizardViewModel : ObservableObject, IPageLifecycleAware
{
    public const int StepCount = 5;

    private readonly IAuthService _authService;
    private readonly IMediator _mediator;
    private readonly INavigator _navigator;
    private readonly IDictationService _dictation;
    private readonly ILogger<PropertyWizardViewModel> _logger;
    private readonly ListingAnalysisRunner _runner;

    private bool _initialized;

    /// <summary>Navigationsparameter: Id eines fortzusetzenden Entwurfs</summary>
    [ShellProperty]
    public string DraftId { get; set; } = string.Empty;

    public MunicipalitySearchModel Ort { get; }

    #region Schritt-Navigation

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMediaStep))]
    [NotifyPropertyChangedFor(nameof(IsDescribeStep))]
    [NotifyPropertyChangedFor(nameof(IsLocationStep))]
    [NotifyPropertyChangedFor(nameof(IsDetailsStep))]
    [NotifyPropertyChangedFor(nameof(IsPreviewStep))]
    [NotifyPropertyChangedFor(nameof(StepProgressText))]
    [NotifyPropertyChangedFor(nameof(ShowNextButton))]
    [NotifyPropertyChangedFor(nameof(Step2Reached))]
    [NotifyPropertyChangedFor(nameof(Step3Reached))]
    [NotifyPropertyChangedFor(nameof(Step4Reached))]
    [NotifyPropertyChangedFor(nameof(Step5Reached))]
    public partial int CurrentStep { get; set; }

    public bool IsMediaStep => CurrentStep == 0;
    public bool IsDescribeStep => CurrentStep == 1;
    public bool IsLocationStep => CurrentStep == 2;
    public bool IsDetailsStep => CurrentStep == 3;
    public bool IsPreviewStep => CurrentStep == 4;

    public string StepProgressText => $"Schritt {CurrentStep + 1} von {StepCount}";

    /// <summary>Der letzte Schritt hat statt "Weiter" den Veroeffentlichen-Button im Inhalt</summary>
    public bool ShowNextButton => CurrentStep < StepCount - 1;

    // Fortschrittsbalken-Segmente (Segment 1 ist immer erreicht)
    public bool Step2Reached => CurrentStep >= 1;
    public bool Step3Reached => CurrentStep >= 2;
    public bool Step4Reached => CurrentStep >= 3;
    public bool Step5Reached => CurrentStep >= 4;

    #endregion

    #region UI-State

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string? BusyMessage { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; set; }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    #endregion

    public PropertyWizardViewModel(
        IAuthService authService,
        IMediator mediator,
        INavigator navigator,
        ILocationService locationService,
        IDictationService dictation,
        ILogger<PropertyWizardViewModel> logger)
    {
        _authService = authService;
        _mediator = mediator;
        _navigator = navigator;
        _dictation = dictation;
        _logger = logger;

        Ort = new MunicipalitySearchModel(locationService, logger);
        _runner = new ListingAnalysisRunner(mediator, logger);
        _runner.StateChanged += OnRunnerStateChanged;

        InitializeDescribeStep();
        InitializeDetailsStep();
        InitializeLocationPriceStep();
    }

    #region IPageLifecycleAware

    public void OnAppearing()
    {
        // Inserieren erfordert ein angemeldetes Verkaeufer-Konto (API: RequireSeller) -
        // frueh abfangen statt spaeter mit rohem 401/403 zu scheitern
        if (!_authService.IsAuthenticated)
        {
            _ = _navigator.NavigateTo("Login", relativeNavigation: false);
            return;
        }

        if (!_authService.IsSeller)
            ErrorMessage = "Inserate erstellen ist nur mit einem Verkäufer-Konto möglich.";

        SubscribeDictation();

        _ = Ort.EnsureLoadedAsync();

        if (!_initialized)
        {
            _initialized = true;
            if (Guid.TryParse(DraftId, out var draftId))
                _ = LoadDraftAsync(draftId);
        }
    }

    public void OnDisappearing()
    {
        if (IsListening)
            _ = _dictation.StopAsync();

        UnsubscribeDictation();

        // Runner bewusst NICHT abbrechen: OnDisappearing feuert auch, wenn der
        // System-MediaPicker die Seite verdeckt - die Analyse soll weiterlaufen.
    }

    #endregion

    #region Weiter / Zurueck / Abbrechen

    [RelayCommand]
    private async Task NextAsync()
    {
        ErrorMessage = null;

        switch (CurrentStep)
        {
            case 0:
                // Upload eager im Hintergrund starten - nur Items ohne RemoteUrl,
                // daher kein Doppel-Upload beim Zurueckgehen und erneutem Weiter
                StartMediaUpload();
                break;

            case 1:
                if (!await AdvanceFromDescribeAsync(skipAi: false))
                    return;
                break;

            case 2:
                if (!ValidateLocationPrice())
                    return;
                break;

            case 3:
                if (!ValidateDetails())
                    return;
                break;
        }

        await GoToStepAsync(CurrentStep + 1);
    }

    [RelayCommand]
    private async Task BackAsync()
    {
        ErrorMessage = null;

        if (CurrentStep == 0)
        {
            await CancelAsync();
            return;
        }

        await GoToStepAsync(CurrentStep - 1);
    }

    /// <summary>Schrittwechsel inkl. Entwurfs-Speicherung (Fehler blockieren nicht).</summary>
    private async Task GoToStepAsync(int step)
    {
        CurrentStep = Math.Clamp(step, 0, StepCount - 1);
        await SaveDraftAsync();
    }

    /// <summary>
    /// Abbruch ueber Zurueck-Pfeil/Hardware-Back: pristiner Wizard geht direkt zurueck,
    /// sonst 3-Wege-Prompt (Weiter bearbeiten / Als Entwurf speichern / Verwerfen).
    /// </summary>
    [RelayCommand]
    private async Task CancelAsync()
    {
        if (IsBusy)
            return;

        if (!HasAnyInput())
        {
            await DeleteDraftAsync();
            await _navigator.GoBack();
            return;
        }

        var choice = await Shell.Current.DisplayActionSheetAsync(
            "Inserat-Entwurf",
            "Weiter bearbeiten",
            "Verwerfen",
            "Als Entwurf speichern");

        switch (choice)
        {
            case "Als Entwurf speichern":
                _runner.Cancel();
                await SaveDraftAsync();
                await _navigator.GoBack();
                break;

            case "Verwerfen":
                _runner.Cancel();
                await DeleteDraftAsync();
                await _navigator.GoBack();
                break;
        }
    }

    /// <summary>
    /// Von der Page fuer Hardware-/Toolbar-Back aufgerufen.
    /// Liefert true = Navigation wird selbst behandelt (Prompt statt sofortigem Pop).
    /// </summary>
    public bool HandleBackRequested()
    {
        _ = CancelAsync();
        return true;
    }

    /// <summary>True sobald der Nutzer irgendetwas erfasst hat (steuert den Abbruch-Prompt).</summary>
    private bool HasAnyInput() =>
        _serverDraftId != null
        || Media.Count > 0
        || !string.IsNullOrWhiteSpace(DictatedText)
        || !string.IsNullOrWhiteSpace(Adresse)
        || !string.IsNullOrWhiteSpace(Preis)
        || Ort.SelectedGemeindeId != null
        || !string.IsNullOrWhiteSpace(Titel)
        || !string.IsNullOrWhiteSpace(Beschreibung);

    #endregion
}
