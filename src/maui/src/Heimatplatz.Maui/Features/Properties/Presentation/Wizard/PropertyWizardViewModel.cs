using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Maui.Core.Media;
using Heimatplatz.Maui.Features.Auth;
using Heimatplatz.Maui.Features.Properties.Models;
using Heimatplatz.Maui.Features.Properties.Services;
using Heimatplatz.Maui.Localization;
using Heimatplatz.Maui.Localization.Properties;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Maui.Features.Properties.Presentation.Wizard;

/// <summary>
/// WYSIWYG-Editor fuer die Inseratserstellung: Die Seite sieht aus wie die echte
/// Detailansicht (Hero-Foto, Content-Sheet, Stat-Kacheln) - nur dass Preis, Titel,
/// Adresse, Eckdaten, Merkmale und Beschreibung direkt im Layout editierbar sind.
/// Beschreibung: selbst schreiben ODER aus Stichwoertern erstellen lassen; die
/// Generierung laeuft als Server-Hintergrund-Job und blockiert das Veroeffentlichen
/// NICHT (der fertige Text wird dem Inserat serverseitig nachgeliefert).
/// Jede Eingabe speichert sich automatisch als Server-Entwurf (debounced);
/// Abbruch bietet Speichern/Verwerfen an, Fortsetzen laeuft ueber DraftId.
/// Aufgeteilt in partial-Dateien (.Media/.Details/.LocationPrice/.Description/.Draft/.Preview).
/// </summary>
[ShellMap<PropertyWizardPage>("PropertyWizard")]
public partial class PropertyWizardViewModel : ObservableObject, IPageLifecycleAware
{
    private static readonly TimeSpan AutoSaveDebounce = TimeSpan.FromSeconds(3);

    /// <summary>Eingaben, die den Entwurf veraendern und die Live-Anzeige aktualisieren</summary>
    private static readonly HashSet<string> EditorInputProperties =
    [
        nameof(Titel), nameof(Zimmer), nameof(Wohnflaeche), nameof(Grundstuecksflaeche),
        nameof(Baujahr), nameof(Preis), nameof(Adresse), nameof(Beschreibung),
        nameof(DescriptionKeywords), nameof(DescriptionMode), nameof(SelectedPropertyTypeItem),
        nameof(GenerationStatus)
    ];

    private readonly IAuthService _authService;
    private readonly IMediator _mediator;
    private readonly INavigator _navigator;
    private readonly IDictationService _dictation;
    private readonly IPhotoPreviewService _photoPreview;
    private readonly CommonStringsLocalized _common;
    private readonly ILogger<PropertyWizardViewModel> _logger;

    /// <summary>Lokalisierte Texte des Editors (auch von allen partial-Dateien und im XAML genutzt)</summary>
    public WizardStringsLocalized Loc { get; }

    private CancellationTokenSource? _autoSaveCts;
    private bool _initialized;

    /// <summary>Navigationsparameter: Id eines fortzusetzenden Entwurfs</summary>
    [ShellProperty]
    public string DraftId { get; set; } = string.Empty;

    /// <summary>Navigationsparameter: Id einer bestehenden Immobilie zum Bearbeiten (Edit-Modus)</summary>
    [ShellProperty]
    public string EditPropertyId { get; set; } = string.Empty;

    public MunicipalitySearchModel Ort { get; }

    #region Modus (Erstellen vs. Bearbeiten)

    /// <summary>
    /// Edit-Modus: gleiche WYSIWYG-Ansicht, aber ohne Server-Entwuerfe und ohne
    /// KI-Generierung - gespeichert wird direkt via UpdateProperty.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCreateMode))]
    [NotifyPropertyChangedFor(nameof(PageTitle))]
    [NotifyPropertyChangedFor(nameof(PublishButtonText))]
    [NotifyPropertyChangedFor(nameof(PublishReadyText))]
    public partial bool IsEditMode { get; set; }

    public bool IsCreateMode => !IsEditMode;

    public string PageTitle => IsEditMode ? Loc.PageTitleEdit : Loc.PageTitleCreate;

    /// <summary>Ungespeicherte Aenderungen im Edit-Modus (steuert den Abbruch-Prompt)</summary>
    private bool _editDirty;

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
        IPhotoPreviewService photoPreview,
        WizardStringsLocalized loc,
        CommonStringsLocalized common,
        ILogger<PropertyWizardViewModel> logger)
    {
        _authService = authService;
        _mediator = mediator;
        _navigator = navigator;
        _dictation = dictation;
        _photoPreview = photoPreview;
        Loc = loc;
        _common = common;
        _logger = logger;

        // Vor InitializeDetailsStep befuellen - dort wird der Standard-Typ gesetzt
        PropertyTypes = PropertyTypeItem.GetAll(loc.PropertyTypeHouse, loc.PropertyTypeLand);

        Ort = new MunicipalitySearchModel(locationService, logger);

        InitializeDetailsStep();
        InitializeLocationPriceStep();
        InitializeDescriptionStep();

        PropertyChanged += OnEditorPropertyChanged;
        Ort.PropertyChanged += OnOrtPropertyChanged;
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
            ErrorMessage = Loc.SellerAccountRequired;

        SubscribeDictation();

        _ = Ort.EnsureLoadedAsync();

        if (!_initialized)
        {
            _initialized = true;
            if (Guid.TryParse(EditPropertyId, out var editPropertyId))
            {
                IsEditMode = true;
                _ = LoadPropertyForEditAsync(editPropertyId);
            }
            else if (Guid.TryParse(DraftId, out var draftId))
            {
                _ = LoadDraftAsync(draftId);
            }
        }
    }

    public void OnDisappearing()
    {
        if (IsListening)
            _ = _dictation.StopAsync();

        UnsubscribeDictation();

        // Offene Aenderungen sofort sichern (der Debounce-Timer wuerde sonst verpuffen)
        FlushAutoSave();

        // Beschreibungs-Polling bewusst NICHT stoppen: OnDisappearing feuert auch, wenn der
        // System-MediaPicker die Seite verdeckt - der Job laeuft serverseitig ohnehin weiter.
    }

    #endregion

    #region Auto-Save (debounced)

    private void OnEditorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not { } name || !EditorInputProperties.Contains(name))
            return;

        RefreshEditorState();
        ScheduleAutoSave();
    }

    private void OnOrtPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MunicipalitySearchModel.SelectedGemeindeId))
            return;

        RefreshEditorState();
        ScheduleAutoSave();
    }

    /// <summary>Speichert den Entwurf nach kurzer Tipp-Pause (jede Eingabe startet den Timer neu).</summary>
    internal void ScheduleAutoSave()
    {
        // Edit-Modus kennt keine Server-Entwuerfe: Eingaben nur als "ungespeichert" merken
        if (IsEditMode)
        {
            _editDirty = true;
            return;
        }

        _autoSaveCts?.Cancel();
        _autoSaveCts?.Dispose();
        var cts = new CancellationTokenSource();
        _autoSaveCts = cts;

        _ = AutoSaveAfterDelayAsync(cts.Token);
    }

    private async Task AutoSaveAfterDelayAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(AutoSaveDebounce, ct);
            await SaveDraftAsync();
        }
        catch (OperationCanceledException)
        {
            // Neuere Eingabe hat den Timer neu gestartet
        }
    }

    /// <summary>Bricht den Debounce ab und speichert sofort (Seite verlassen).</summary>
    private void FlushAutoSave()
    {
        if (_autoSaveCts == null)
            return;

        CancelPendingAutoSave();
        _ = SaveDraftAsync();
    }

    /// <summary>
    /// Verwirft einen anstehenden Auto-Save ersatzlos - nach Veroeffentlichen/Verwerfen
    /// existiert der Server-Entwurf nicht mehr, ein spaeter feuernder Save liefe ins Leere.
    /// </summary>
    private void CancelPendingAutoSave()
    {
        _autoSaveCts?.Cancel();
        _autoSaveCts?.Dispose();
        _autoSaveCts = null;
    }

    #endregion

    #region Abbrechen

    /// <summary>
    /// Abbruch ueber Zurueck-Pfeil/Hardware-Back: pristiner Editor geht direkt zurueck,
    /// sonst 3-Wege-Prompt (Weiter bearbeiten / Als Entwurf speichern / Verwerfen).
    /// </summary>
    [RelayCommand]
    private async Task CancelAsync()
    {
        if (IsBusy)
            return;

        // Edit-Modus: kein Entwurfs-Handling - ohne Aenderungen direkt zurueck,
        // sonst kurz nachfragen (Zurueck wuerde die Eingaben verwerfen)
        if (IsEditMode)
        {
            if (!_editDirty)
            {
                await _navigator.GoBack();
                return;
            }

            var editChoice = await Shell.Current.DisplayActionSheetAsync(
                Loc.DiscardChangesTitle,
                Loc.KeepEditing,
                Loc.Discard);

            if (editChoice == Loc.Discard)
                await _navigator.GoBack();
            return;
        }

        if (!HasAnyInput())
        {
            await DeleteDraftAsync();
            await _navigator.GoBack();
            return;
        }

        var choice = await Shell.Current.DisplayActionSheetAsync(
            Loc.DraftPromptTitle,
            Loc.KeepEditing,
            Loc.Discard,
            Loc.SaveAsDraft);

        // Kein switch: Lokalisierte Texte sind keine Konstanten (case-Labels unzulaessig)
        if (choice == Loc.SaveAsDraft)
        {
            StopDescriptionPolling();
            await SaveDraftAsync();
            await _navigator.GoBack();
        }
        else if (choice == Loc.Discard)
        {
            StopDescriptionPolling();
            CancelPendingAutoSave();
            await DeleteDraftAsync();
            await _navigator.GoBack();
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
        || FeatureItems.Count > 0
        || !string.IsNullOrWhiteSpace(DescriptionKeywords)
        || !string.IsNullOrWhiteSpace(Adresse)
        || !string.IsNullOrWhiteSpace(Preis)
        || Ort.SelectedGemeindeId != null
        || !string.IsNullOrWhiteSpace(Titel)
        || !string.IsNullOrWhiteSpace(Beschreibung);

    #endregion
}
