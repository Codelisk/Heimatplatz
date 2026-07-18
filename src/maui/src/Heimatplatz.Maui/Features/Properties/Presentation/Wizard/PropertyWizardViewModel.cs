using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Maui.Core.Media;
using Heimatplatz.Maui.Features.Auth;
using Heimatplatz.Maui.Features.Properties.Models;
using Heimatplatz.Maui.Features.Properties.Services;
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
    private readonly ILogger<PropertyWizardViewModel> _logger;

    private CancellationTokenSource? _autoSaveCts;
    private bool _initialized;

    /// <summary>Navigationsparameter: Id eines fortzusetzenden Entwurfs</summary>
    [ShellProperty]
    public string DraftId { get; set; } = string.Empty;

    public MunicipalitySearchModel Ort { get; }

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
        ILogger<PropertyWizardViewModel> logger)
    {
        _authService = authService;
        _mediator = mediator;
        _navigator = navigator;
        _dictation = dictation;
        _photoPreview = photoPreview;
        _logger = logger;

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
                StopDescriptionPolling();
                await SaveDraftAsync();
                await _navigator.GoBack();
                break;

            case "Verwerfen":
                StopDescriptionPolling();
                CancelPendingAutoSave();
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
        || FeatureItems.Count > 0
        || !string.IsNullOrWhiteSpace(DescriptionKeywords)
        || !string.IsNullOrWhiteSpace(Adresse)
        || !string.IsNullOrWhiteSpace(Preis)
        || Ort.SelectedGemeindeId != null
        || !string.IsNullOrWhiteSpace(Titel)
        || !string.IsNullOrWhiteSpace(Beschreibung);

    #endregion
}
