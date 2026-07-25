using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Core.Collections;
using Heimatplatz.Maui.Features.Auth;
using Heimatplatz.Maui.Features.Properties.Models;
using Heimatplatz.Maui.Features.Properties.Services;
using Heimatplatz.Maui.Features.Properties.Sync;
using Heimatplatz.Maui.Localization;
using Heimatplatz.Maui.Localization.Properties;
using Heimatplatz.Maui.Offline;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Maui.Features.Properties.Presentation;

/// <summary>
/// ViewModel fuer die HomePage (Immobilien-Liste mit Pull-to-Refresh,
/// expliziter API-Pagination und Sortierung).
/// Wird als ShellContent "MainPage" eingebunden (registerRoute: false).
/// </summary>
[ShellMap<HomePage>(registerRoute: false)]
public partial class HomeViewModel : ObservableObject, IPageLifecycleAware, IDisposable
{
#if DEBUG
    private const string DebugMockCountPreferenceKey = "debug.properties.mock-count";
    private const string DebugMockPaginationPreferenceKey = "debug.properties.mock-pagination";
#endif

    private readonly IAuthService _authService;
    private readonly INavigator _navigator;
    private readonly IDialogs _dialogs;
    private readonly IFilterPreferencesService _filterPreferencesService;
    private readonly IFilterStateService _filterStateService;
    private readonly IPropertyStatusService _propertyStatusService;
    private readonly ILocationService _locationService;
    private readonly IMediator _mediator;
    private readonly OfflineReadState _offlineReadState;
    private readonly PropertyDetailPreloader _detailPreloader;
    private readonly ILogger<HomeViewModel> _logger;
    private readonly HomeStringsLocalized _loc;
    // Dialog-Button-Texte (OK) - Shiny-Defaults sind englisch
    private readonly CommonStringsLocalized _commonLoc;

    private int _currentPage;
    private int _totalCount;
    private SortOption _selectedSort = SortOption.Neueste;
    private AgeFilter _selectedAgeFilter = AgeFilter.Alle;
    private List<Guid> _excludedSellerSourceIds = [];
    private bool _isSyncing;
    private IDisposable? _syncSubscription;
    private CancellationTokenSource? _saveDebounceCts;
    private Task? _filterPreferencesLoadTask;
    private bool _filterPreferencesLoaded;
#if DEBUG
    private bool _debugMockPreferencesChecked;
    private bool _isShowingAllDebugMock;
    private List<PropertyListItemDto>? _debugMockProperties;
#endif

    /// <summary>Lokalisierte Texte fuer XAML-Bindings (Loc.Key)</summary>
    public HomeStringsLocalized Loc => _loc;

    /// <summary>
    /// Immer dieselbe Collection-Instanz: Seitenwechsel/Reloads ersetzen den Inhalt
    /// per ReplaceRange (eine Reset-Notification), damit die CollectionView ihren
    /// Container-Recycling-Pool behaelt statt alles neu zu realisieren.
    /// </summary>
    public ObservableRangeCollection<PropertyListItemDto> Properties { get; } = [];

    /// <summary>
    /// Bittet die Page, an den Listenanfang zu scrollen. Noetig weil die ItemsSource-
    /// Instanz gleich bleibt - die Scroll-Position wuerde einen Seitenwechsel sonst ueberleben.
    /// </summary>
    public event EventHandler? ScrollToTopRequested;

    [ObservableProperty]
    public partial int SelectedPageSize { get; set; }

    public int PageCount => _totalCount == 0
        ? 0
        : (int)Math.Ceiling(_totalCount / (double)SelectedPageSize);

    /// <summary>Footer-Text: Treffer-Anzahl, bei mehreren Seiten inkl. Seitenzahl</summary>
    public string PageNumberText => PageCount <= 1
        ? FormatObjektCount(_totalCount)
        : _loc.PageNumberFormat(_currentPage + 1, PageCount, FormatObjektCount(_totalCount));

    public bool HasResults => _totalCount > 0;
    public bool HasPagination =>
#if DEBUG
        !_isShowingAllDebugMock &&
#endif
        PageCount > 1;
    public bool CanGoToPreviousPage => _currentPage > 0;
    public bool CanGoToNextPage => _currentPage + 1 < PageCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowRegularEmptyState))]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string? BusyMessage { get; set; }

    [ObservableProperty]
    public partial bool IsRefreshing { get; set; }

    [ObservableProperty]
    public partial bool IsShowingCachedData { get; set; }

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    /// <summary>
    /// Fehlermeldung wenn das Laden der Liste fehlschlaegt. Wird inline im Listenbereich
    /// angezeigt (mit Retry-Button) statt als modaler Dialog - ein Dialog wuerde beim
    /// App-Start jede Eingabe blockieren und das Busy-Overlay festhaengen lassen.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLoadError))]
    [NotifyPropertyChangedFor(nameof(HasNoLoadError))]
    [NotifyPropertyChangedFor(nameof(ShowRegularEmptyState))]
    public partial string? LoadErrorMessage { get; set; }

    public bool HasLoadError => !string.IsNullOrWhiteSpace(LoadErrorMessage);

    public bool HasNoLoadError => !HasLoadError;

    /// <summary>
    /// "Keine Treffer" nur zeigen, wenn weder ein Fehler ansteht noch geladen wird -
    /// sonst liegen Empty-State und Busy-Overlay beim Start uebereinander.
    /// </summary>
    public bool ShowRegularEmptyState => HasNoLoadError && !IsBusy;

    [ObservableProperty]
    public partial bool IsAuthenticated { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSellerFilterVisible))]
    public partial bool IsHausSelected { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSellerFilterVisible))]
    public partial bool IsGrundstueckSelected { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSellerFilterVisible))]
    public partial bool IsZwangsversteigerungSelected { get; set; }

    [ObservableProperty]
    public partial bool IsPrivateSelected { get; set; }

    [ObservableProperty]
    public partial bool IsBrokerSelected { get; set; }

    /// <summary>
    /// Bei Zwangsversteigerungen gibt es keinen Anbieter-Unterschied (immer Gericht/Edikt) -
    /// Anbieter-Filter ist dann wirkungslos und wird ausgeblendet statt eine tote Auswahl zu zeigen.
    /// </summary>
    public bool IsSellerFilterVisible => !(IsZwangsversteigerungSelected && !IsHausSelected && !IsGrundstueckSelected);

    /// <summary>
    /// Optionen fuer den Alters-Filter Picker (Index == AgeFilter Enum-Wert)
    /// </summary>
    public IReadOnlyList<string> AgeFilterOptions { get; }

    [ObservableProperty]
    public partial int SelectedAgeFilterIndex { get; set; }

    /// <summary>
    /// Kurze Zusammenfassung der aktiven Filter fuer den eingeklappten Filter-Header
    /// </summary>
    [ObservableProperty]
    public partial string FilterSummary { get; set; }

    // Ort-Auswahl (Filter-Zustand; die Auswahl selbst erfolgt im Ort-Panel)
    public ObservableCollection<string> SelectedOrte { get; } = [];

    /// <summary>Anzeige-Chips der aktiven Ort-Auswahl (komplett gewaehlte Bezirke zusammengefasst)</summary>
    public ObservableCollection<OrtChip> OrtChips { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedOrte))]
    [NotifyPropertyChangedFor(nameof(OrtFieldLabel))]
    public partial int SelectedOrteCount { get; set; }

    public bool HasSelectedOrte => SelectedOrteCount > 0;

    /// <summary>Beschriftung des Ort-Auswahl-Felds in der Filterleiste</summary>
    public string OrtFieldLabel => SelectedOrteCount switch
    {
        0 => _loc.OrtFieldNone,
        1 => SelectedOrte[0],
        _ => _loc.OrtFieldManyFormat(SelectedOrteCount)
    };

    // Ort-Auswahl-Panel (Bottom Sheet): Bezirk->Gemeinde-Baum als Arbeitskopie
    public ObservableCollection<OrtBezirkItem> OrtBezirke { get; } = [];

    [ObservableProperty]
    public partial bool IsOrtPanelOpen { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOrtPanelSearchActive))]
    [NotifyPropertyChangedFor(nameof(IsOrtPanelBrowseVisible))]
    public partial string OrtPanelSearchText { get; set; }

    [ObservableProperty]
    public partial List<OrtGemeindeItem> OrtPanelSearchResults { get; set; }

    /// <summary>Text des "Übernehmen"-Buttons inkl. Treffer-Vorschau</summary>
    [ObservableProperty]
    public partial string OrtPanelApplyText { get; set; }

    /// <summary>True sobald im Panel gesucht wird - zeigt Suchergebnisse statt Bezirk-Liste</summary>
    public bool IsOrtPanelSearchActive => OrtPanelSearchText.Trim().Length >= 2;

    public bool IsOrtPanelBrowseVisible => !IsOrtPanelSearchActive;

    // ---- Chip-Zeile: kleine Bottom-Sheets fuer Sortierung/Typ/Zeitraum ----

    [ObservableProperty]
    public partial bool IsSortSheetOpen { get; set; }

    [ObservableProperty]
    public partial bool IsTypeSheetOpen { get; set; }

    [ObservableProperty]
    public partial bool IsAgeSheetOpen { get; set; }

    /// <summary>Enum-Name der aktiven Sortierung (fuer das Haekchen im Sortier-Sheet)</summary>
    public string SelectedSortName => _selectedSort.ToString();

    /// <summary>Beschriftung des Sortier-Chips</summary>
    public string SortChipLabel => GetSortLabel(_selectedSort);

    /// <summary>Beschriftung des Typ-Chips (kurz, damit die Chip-Zeile nicht ueberlaeuft)</summary>
    public string TypeChipLabel
    {
        get
        {
            var types = new List<string>(3);
            if (IsHausSelected) types.Add(_loc.TypeShortHouse);
            if (IsGrundstueckSelected) types.Add(_loc.TypeShortLand);
            if (IsZwangsversteigerungSelected) types.Add(_loc.TypeShortForeclosure);
            return types.Count is 0 or 3 ? _loc.AllTypes : string.Join(" · ", types);
        }
    }

    public bool IsTypeFilterActive => !(IsHausSelected && IsGrundstueckSelected && IsZwangsversteigerungSelected);

    public string AgeChipLabel => SelectedAgeFilterIndex <= 0 ? _loc.ChipAgeDefault : AgeFilterOptions[SelectedAgeFilterIndex];

    public bool IsAgeFilterActive => SelectedAgeFilterIndex > 0;

    public string OrtChipLabel => SelectedOrteCount switch
    {
        0 => _loc.AllOrte,
        1 => SelectedOrte[0],
        _ => _loc.OrteCountFormat(SelectedOrteCount)
    };

    public bool IsOrtFilterActive => SelectedOrteCount > 0;

    /// <summary>Chip-Beschriftungen und Aktiv-Zustaende nach jeder Filteraenderung nachziehen</summary>
    private void RefreshChipLabels()
    {
        OnPropertyChanged(nameof(SelectedSortName));
        OnPropertyChanged(nameof(SortChipLabel));
        OnPropertyChanged(nameof(TypeChipLabel));
        OnPropertyChanged(nameof(IsTypeFilterActive));
        OnPropertyChanged(nameof(AgeChipLabel));
        OnPropertyChanged(nameof(IsAgeFilterActive));
        OnPropertyChanged(nameof(OrtChipLabel));
        OnPropertyChanged(nameof(IsOrtFilterActive));
    }

    public HomeViewModel(
        IAuthService authService,
        INavigator navigator,
        IDialogs dialogs,
        IFilterPreferencesService filterPreferencesService,
        IFilterStateService filterStateService,
        IPropertyStatusService propertyStatusService,
        ILocationService locationService,
        IMediator mediator,
        OfflineReadState offlineReadState,
        ILogger<HomeViewModel> logger,
        HomeStringsLocalized loc,
        CommonStringsLocalized commonLoc,
        PropertyDetailPreloader detailPreloader)
    {
        _detailPreloader = detailPreloader;
        _authService = authService;
        _navigator = navigator;
        _dialogs = dialogs;
        _filterPreferencesService = filterPreferencesService;
        _filterStateService = filterStateService;
        _propertyStatusService = propertyStatusService;
        _locationService = locationService;
        _mediator = mediator;
        _offlineReadState = offlineReadState;
        _logger = logger;
        _loc = loc;
        _commonLoc = commonLoc;

        AgeFilterOptions = [loc.AgeOptionAll, loc.AgeOptionDay, loc.AgeOptionWeek, loc.AgeOptionMonth, loc.AgeOptionYear];

        // Initialwerte (partial properties koennen keine Initializer haben);
        // Zwangsversteigerungen standardmaessig deaktiviert (wie Web/API)
        _isSyncing = true;
        IsHausSelected = true;
        IsGrundstueckSelected = true;
        IsZwangsversteigerungSelected = false;
        IsPrivateSelected = true;
        IsBrokerSelected = true;
        SelectedAgeFilterIndex = 0;
        OrtPanelSearchText = string.Empty;
        OrtPanelSearchResults = [];
        OrtPanelApplyText = loc.Apply;
        FilterSummary = string.Empty;
        SelectedPageSize = PageSizePreference.Get();
        _isSyncing = false;

        UpdateFilterSummary();

        _authService.AuthenticationStateChanged += OnAuthenticationStateChanged;
        _filterStateService.FilterStateChanged += OnFilterStateChanged;

        // Delta-Sync: sichtbare Liste in-place patchen; nur bei neuen Immobilien
        // wird die aktuelle Seite einmal frisch geladen (Filter-Einordnung im Backend)
        _syncSubscription = _mediator.Subscribe<PropertyDataSyncedEvent>((evt, _, _) =>
        {
            MainThread.BeginInvokeOnMainThread(() => ApplySyncedChanges(evt));
            return Task.CompletedTask;
        });

        UpdateAuthState();

        // Den Ort-Baum bewusst NICHT hier aufbauen. Das geschlossene Bottom Sheet
        // wuerde sonst beim Homepage-Aufbau bereits hunderte Gemeinde-Views erzeugen.
        // PropertyFilterRequestBuilder und OpenOrtPanelAsync laden die Daten bei Bedarf.
    }

    #region IPageLifecycleAware

    public void OnAppearing()
    {
        // Hintergrund-Refreshes melden Serverausfaelle erst NACH dem synchronen
        // Cache-Hit - den Hinweis "zwischengespeicherte Daten" live nachziehen.
        _offlineReadState.Changed += OnOfflineReadStateChanged;
        IsShowingCachedData = _offlineReadState.IsBackendUnavailable;

        // Session-Filter-State wiederherstellen (z.B. nach Rueckkehr von einer Detail-Seite)
        SyncFiltersFromService();

        // "Pro Seite" wird auf der FilterSettingsPage geaendert - beim Zurueckkommen
        // uebernehmen (der Property-Setter persistiert und laedt neu)
        var storedPageSize = PageSizePreference.Get();
        if (storedPageSize != SelectedPageSize)
            SelectedPageSize = storedPageSize;

#if DEBUG
        // Erlaubt reproduzierbare Emulator-Stresstests auch dann, wenn DevFlow-Actions
        // wegen eingebetteter Android-Assemblies nicht reflektiert werden koennen.
        if (!_debugMockPreferencesChecked)
        {
            _debugMockPreferencesChecked = true;
            var mockCount = Preferences.Default.Get(DebugMockCountPreferenceKey, 0);
            if (mockCount > 0)
            {
                var usePagination = Preferences.Default.Get(DebugMockPaginationPreferenceKey, false);
                _ = LoadDebugMockPropertiesAsync(mockCount, usePagination);
                return;
            }
        }
#endif

        if (Properties.Count == 0 && !IsBusy)
        {
            _ = ReloadPropertiesAsync();
        }

        if (_authService.IsAuthenticated)
        {
            _ = LoadFilterPreferencesAsync();
            _ = _propertyStatusService.EnsureLoadedAsync();
        }
    }

    public void OnDisappearing()
    {
        _offlineReadState.Changed -= OnOfflineReadStateChanged;
    }

    private void OnOfflineReadStateChanged(object? sender, EventArgs e) =>
        MainThread.BeginInvokeOnMainThread(() =>
            IsShowingCachedData = _offlineReadState.IsBackendUnavailable);

    #endregion

    #region Filter-Synchronisation

    private void OnFilterStateChanged(object? sender, EventArgs e)
    {
        SyncFiltersFromService();
        _ = ReloadPropertiesAsync();
    }

    /// <summary>
    /// Synchronisiert die Filterwerte vom FilterStateService ohne Reload auszuloesen.
    /// </summary>
    private void SyncFiltersFromService()
    {
        if (!_filterStateService.HasSessionState) return;

        var state = _filterStateService.CurrentState;

        _isSyncing = true;
        try
        {
            IsHausSelected = state.IsHausSelected;
            IsGrundstueckSelected = state.IsGrundstueckSelected;
            IsZwangsversteigerungSelected = state.IsZwangsversteigerungSelected;
            IsPrivateSelected = state.IsPrivateSelected;
            IsBrokerSelected = state.IsBrokerSelected;
            _selectedAgeFilter = state.SelectedAgeFilter;
            SelectedAgeFilterIndex = (int)state.SelectedAgeFilter;
            ReplaceSelectedOrte(state.SelectedOrte);
            _excludedSellerSourceIds = state.ExcludedSellerSourceIds.ToList();
            _selectedSort = state.SelectedSort;
            UpdateFilterSummary();
        }
        finally
        {
            _isSyncing = false;
        }
    }

    private Task LoadFilterPreferencesAsync()
    {
        if (_filterPreferencesLoaded)
            return Task.CompletedTask;

        return _filterPreferencesLoadTask ??= LoadFilterPreferencesCoreAsync();
    }

    private async Task LoadFilterPreferencesCoreAsync()
    {
        try
        {
            var preferences = await _filterPreferencesService.GetPreferencesAsync();
            _filterPreferencesLoaded = true;
            if (preferences != null && ApplyFilterPreferences(preferences))
            {
                _logger.LogInformation("[HomePage] Gespeicherte Filter unterscheiden sich - lade Treffer einmal neu");
                await ReloadPropertiesAsync();
            }
            else
            {
                _logger.LogInformation("[HomePage] Gespeicherte Filter unveraendert - kein zusaetzlicher Reload");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[HomePage] Failed to load filter preferences");
        }
        finally
        {
            _filterPreferencesLoadTask = null;
        }
    }

    private bool ApplyFilterPreferences(FilterPreferencesDto preferences)
    {
        var selectedOrteChanged = !SelectedOrte.ToHashSet(StringComparer.OrdinalIgnoreCase)
            .SetEquals(preferences.SelectedOrte);
        var excludedSellersChanged = !_excludedSellerSourceIds.ToHashSet()
            .SetEquals(preferences.ExcludedSellerSourceIds);
        var changed = IsHausSelected != preferences.IsHausSelected
            || IsGrundstueckSelected != preferences.IsGrundstueckSelected
            || IsZwangsversteigerungSelected != preferences.IsZwangsversteigerungSelected
            || IsPrivateSelected != preferences.IsPrivateSelected
            || IsBrokerSelected != preferences.IsBrokerSelected
            || _selectedAgeFilter != preferences.SelectedAgeFilter
            || _selectedSort != preferences.SelectedSort
            || selectedOrteChanged
            || excludedSellersChanged;

        if (!changed)
            return false;

        _isSyncing = true;
        try
        {
            IsHausSelected = preferences.IsHausSelected;
            IsGrundstueckSelected = preferences.IsGrundstueckSelected;
            IsZwangsversteigerungSelected = preferences.IsZwangsversteigerungSelected;
            IsPrivateSelected = preferences.IsPrivateSelected;
            IsBrokerSelected = preferences.IsBrokerSelected;
            _selectedAgeFilter = preferences.SelectedAgeFilter;
            SelectedAgeFilterIndex = (int)preferences.SelectedAgeFilter;
            ReplaceSelectedOrte(preferences.SelectedOrte);
            _excludedSellerSourceIds = preferences.ExcludedSellerSourceIds.ToList();
            _selectedSort = preferences.SelectedSort;
            UpdateFilterSummary();
        }
        finally
        {
            _isSyncing = false;
        }

        // Die separate Filterseite liest denselben Session-State. Den eigenen Handler
        // kurz ausnehmen, damit der ohnehin folgende Reload nicht doppelt ausgeloest wird.
        _filterStateService.FilterStateChanged -= OnFilterStateChanged;
        try
        {
            _filterStateService.UpdateFilters(
                IsHausSelected,
                IsGrundstueckSelected,
                IsZwangsversteigerungSelected,
                _selectedAgeFilter,
                SelectedOrte.ToList(),
                IsPrivateSelected,
                IsBrokerSelected,
                preferences.ExcludedSellerSourceIds,
                _selectedSort);
        }
        finally
        {
            _filterStateService.FilterStateChanged += OnFilterStateChanged;
        }

        return true;
    }

    private void ReplaceSelectedOrte(IEnumerable<string> orte)
    {
        SelectedOrte.Clear();
        foreach (var ort in orte)
            SelectedOrte.Add(ort);
        SelectedOrteCount = SelectedOrte.Count;
        OnPropertyChanged(nameof(OrtFieldLabel));
        RebuildOrtChips();
    }

    /// <summary>
    /// Baut die Kurzform der aktiven Filter fuer den eingeklappten Header,
    /// z.B. "Haus, Grundstück · Privat · 1 Woche · 2 Orte".
    /// </summary>
    private void UpdateFilterSummary()
    {
        var parts = new List<string>();

        var types = new List<string>();
        if (IsHausSelected) types.Add(_loc.TypeHouse);
        if (IsGrundstueckSelected) types.Add(_loc.TypeLand);
        if (IsZwangsversteigerungSelected) types.Add(_loc.TypeForeclosure);
        parts.Add(types.Count == 3 ? _loc.AllTypes : string.Join(", ", types));

        if (IsSellerFilterVisible && IsPrivateSelected != IsBrokerSelected)
            parts.Add(IsPrivateSelected ? _loc.SellerPrivate : _loc.SellerBroker);

        if (_selectedAgeFilter != AgeFilter.Alle)
            parts.Add(AgeFilterOptions[(int)_selectedAgeFilter]);

        parts.Add(SelectedOrte.Count == 0
            ? _loc.AllOrte
            : SelectedOrte.Count == 1 ? SelectedOrte[0] : _loc.OrteCountFormat(SelectedOrte.Count));

        FilterSummary = string.Join(" · ", parts);
        RefreshChipLabels();
    }

    partial void OnIsHausSelectedChanged(bool value)
    {
        if (_isSyncing) return;

        // Mindestens ein Filter muss aktiv bleiben
        if (!value && !IsGrundstueckSelected && !IsZwangsversteigerungSelected)
        {
            _isSyncing = true;
            IsHausSelected = true;
            _isSyncing = false;
            return;
        }

        ResetSellerFilterIfOnlyForeclosureSelected();
        OnFiltersChanged();
    }

    partial void OnIsGrundstueckSelectedChanged(bool value)
    {
        if (_isSyncing) return;

        if (!value && !IsHausSelected && !IsZwangsversteigerungSelected)
        {
            _isSyncing = true;
            IsGrundstueckSelected = true;
            _isSyncing = false;
            return;
        }

        ResetSellerFilterIfOnlyForeclosureSelected();
        OnFiltersChanged();
    }

    partial void OnIsZwangsversteigerungSelectedChanged(bool value)
    {
        if (_isSyncing) return;

        if (!value && !IsHausSelected && !IsGrundstueckSelected)
        {
            _isSyncing = true;
            IsZwangsversteigerungSelected = true;
            _isSyncing = false;
            return;
        }

        ResetSellerFilterIfOnlyForeclosureSelected();
        OnFiltersChanged();
    }

    /// <summary>
    /// Wenn nur Zwangsversteigerung ausgewählt ist (Anbieter-Filter dadurch ausgeblendet, siehe
    /// <see cref="IsSellerFilterVisible"/>), darf keine versteckte Anbieter-Auswahl aktive Ergebnisse
    /// stumm ausfiltern - daher auf "alle Anbieter" zurücksetzen.
    /// </summary>
    private void ResetSellerFilterIfOnlyForeclosureSelected()
    {
        if (!IsSellerFilterVisible)
        {
            _isSyncing = true;
            IsPrivateSelected = true;
            IsBrokerSelected = true;
            _isSyncing = false;
        }
    }

    partial void OnIsPrivateSelectedChanged(bool value)
    {
        if (_isSyncing) return;

        // Mindestens ein Anbietertyp muss aktiv bleiben
        if (!value && !IsBrokerSelected)
        {
            _isSyncing = true;
            IsPrivateSelected = true;
            _isSyncing = false;
            return;
        }

        OnFiltersChanged();
    }

    partial void OnIsBrokerSelectedChanged(bool value)
    {
        if (_isSyncing) return;

        if (!value && !IsPrivateSelected)
        {
            _isSyncing = true;
            IsBrokerSelected = true;
            _isSyncing = false;
            return;
        }

        OnFiltersChanged();
    }

    partial void OnSelectedAgeFilterIndexChanged(int value)
    {
        if (_isSyncing) return;

        _selectedAgeFilter = value >= 0 && value <= (int)AgeFilter.EinJahr
            ? (AgeFilter)value
            : AgeFilter.Alle;

        OnFiltersChanged();
    }

    /// <summary>
    /// Wird bei jeder Filteraenderung aufgerufen - aktualisiert den FilterStateService,
    /// speichert die Einstellungen (debounced, server-seitig) und loest einen Reload aus.
    /// </summary>
    private void OnFiltersChanged()
    {
        if (!_isSyncing)
        {
            // Event-Handler temporaer nicht reagieren lassen (UpdateFilters feuert FilterStateChanged)
            _filterStateService.FilterStateChanged -= OnFilterStateChanged;
            try
            {
                _filterStateService.UpdateFilters(
                    IsHausSelected,
                    IsGrundstueckSelected,
                    IsZwangsversteigerungSelected,
                    _selectedAgeFilter,
                    SelectedOrte.ToList(),
                    IsPrivateSelected,
                    IsBrokerSelected,
                    _excludedSellerSourceIds,
                    selectedSort: _selectedSort);
            }
            finally
            {
                _filterStateService.FilterStateChanged += OnFilterStateChanged;
            }

            ScheduleAutoSave();
        }

        UpdateFilterSummary();
        _ = ReloadPropertiesAsync();
    }

    /// <summary>
    /// Speichert die Filtereinstellungen nach kurzer Verzoegerung (Debounce) server-seitig,
    /// damit sie ueber App-Starts hinweg erhalten bleiben.
    /// </summary>
    private void ScheduleAutoSave()
    {
        if (!_authService.IsAuthenticated) return;

        _saveDebounceCts?.Cancel();
        _saveDebounceCts = new CancellationTokenSource();
        _ = AutoSaveAfterDelayAsync(_saveDebounceCts.Token);
    }

    private async Task AutoSaveAfterDelayAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(800, token);
            if (token.IsCancellationRequested) return;

            var preferences = new FilterPreferencesDto(
                SelectedOrte: SelectedOrte.ToList(),
                SelectedAgeFilter: _selectedAgeFilter,
                IsHausSelected: IsHausSelected,
                IsGrundstueckSelected: IsGrundstueckSelected,
                IsZwangsversteigerungSelected: IsZwangsversteigerungSelected,
                IsPrivateSelected: IsPrivateSelected,
                IsBrokerSelected: IsBrokerSelected,
                ExcludedSellerSourceIds: _excludedSellerSourceIds,
                SelectedSort: _selectedSort);

            await _filterPreferencesService.SavePreferencesAsync(preferences, token);
            _logger.LogInformation("[HomePage] Filter preferences auto-saved");
        }
        catch (OperationCanceledException)
        {
            // Debounce abgebrochen - ignorieren
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[HomePage] Filter preferences auto-save failed");
        }
    }

    #endregion

    #region Ort-Auswahl (Bottom Sheet)

    private Task? _ortTreeLoadTask;
    private CancellationTokenSource? _ortCountCts;

    /// <summary>
    /// Baut den Bezirk->Gemeinde-Baum fuer das Ort-Panel genau einmal auf (single-flight).
    /// Nach einem Fehlschlag (leere Location-Liste) wird beim naechsten Aufruf erneut versucht.
    /// </summary>
    private Task EnsureOrtTreeAsync()
    {
        if (OrtBezirke.Count > 0)
            return Task.CompletedTask;

        if (_ortTreeLoadTask is { IsCompleted: false })
            return _ortTreeLoadTask;

        _ortTreeLoadTask = BuildOrtTreeAsync();
        return _ortTreeLoadTask;
    }

    private async Task BuildOrtTreeAsync()
    {
        var locations = await _locationService.GetLocationsAsync();

        var bezirke = locations
            .SelectMany(bl => bl.Bezirke)
            .OrderBy(bz => bz.Name, StringComparer.CurrentCulture)
            .Select(bz =>
            {
                var gemeinden = bz.Gemeinden
                    .OrderBy(g => g.Name, StringComparer.CurrentCulture)
                    .Select(g => new OrtGemeindeItem { Name = g.Name, PostalCode = g.PostalCode })
                    .ToList();
                var bezirk = new OrtBezirkItem
                {
                    Name = bz.Name,
                    Gemeinden = gemeinden,
                    CountLabelFormatter = count => _loc.SelectedCountFormat(count)
                };
                foreach (var gemeinde in gemeinden)
                    gemeinde.Bezirk = bezirk;
                return bezirk;
            });

        OrtBezirke.Clear();
        foreach (var bezirk in bezirke)
            OrtBezirke.Add(bezirk);

        // Chips neu gruppieren - gespeicherte Filter koennen vor dem Baum angekommen sein
        RebuildOrtChips();
    }

    [RelayCommand]
    private async Task OpenOrtPanelAsync()
    {
        await EnsureOrtTreeAsync();

        OrtPanelSearchText = string.Empty;
        OrtPanelSearchResults = [];
        SyncOrtPanelFromSelection();
        IsOrtPanelOpen = true;
        ScheduleOrtCountPreview();
    }

    /// <summary>
    /// Uebertraegt die aktive Filter-Auswahl in die Arbeitskopie des Panels.
    /// Teilweise gewaehlte Bezirke werden aufgeklappt, damit die Auswahl sichtbar ist.
    /// </summary>
    private void SyncOrtPanelFromSelection()
    {
        var selected = SelectedOrte.ToHashSet();
        foreach (var bezirk in OrtBezirke)
        {
            foreach (var gemeinde in bezirk.Gemeinden)
                gemeinde.IsSelected = selected.Contains(gemeinde.Name);
            bezirk.RefreshSelectedCount();
            bezirk.IsExpanded = bezirk.HasSelection && !bezirk.IsAllSelected;
        }
    }

    partial void OnIsOrtPanelOpenChanged(bool value)
    {
        // Backdrop-Tap/Zuziehen verwirft die Arbeitskopie; naechstes Oeffnen synchronisiert neu
        if (!value)
            _ortCountCts?.Cancel();
    }

    partial void OnOrtPanelSearchTextChanged(string value)
    {
        var search = value.Trim();
        if (search.Length < 2)
        {
            OrtPanelSearchResults = [];
            return;
        }

        OrtPanelSearchResults = OrtBezirke
            .SelectMany(b => b.Gemeinden)
            .Where(g => g.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                     || g.PostalCode.StartsWith(search, StringComparison.OrdinalIgnoreCase))
            .Take(30)
            .ToList();
    }

    [RelayCommand]
    private void ToggleBezirkExpanded(OrtBezirkItem bezirk)
        => bezirk.IsExpanded = !bezirk.IsExpanded;

    /// <summary>Sammel-Checkbox: waehlt alle Gemeinden eines Bezirks an bzw. ab</summary>
    [RelayCommand]
    private void ToggleBezirkSelection(OrtBezirkItem bezirk)
    {
        var target = !bezirk.IsAllSelected;
        foreach (var gemeinde in bezirk.Gemeinden)
            gemeinde.IsSelected = target;
        bezirk.RefreshSelectedCount();
        ScheduleOrtCountPreview();
    }

    [RelayCommand]
    private void ToggleOrtGemeinde(OrtGemeindeItem gemeinde)
    {
        gemeinde.IsSelected = !gemeinde.IsSelected;
        gemeinde.Bezirk?.RefreshSelectedCount();
        ScheduleOrtCountPreview();
    }

    [RelayCommand]
    private void ResetOrtPanel()
    {
        foreach (var bezirk in OrtBezirke)
        {
            foreach (var gemeinde in bezirk.Gemeinden)
                gemeinde.IsSelected = false;
            bezirk.SelectedCount = 0;
        }
        ScheduleOrtCountPreview();
    }

    /// <summary>Uebernimmt die Arbeitskopie aus dem Panel in den Filter und laedt neu</summary>
    [RelayCommand]
    private void ApplyOrtPanel()
    {
        var names = OrtBezirke
            .SelectMany(b => b.Gemeinden)
            .Where(g => g.IsSelected)
            .Select(g => g.Name)
            .ToList();

        ReplaceSelectedOrte(names);
        IsOrtPanelOpen = false;
        OnFiltersChanged();
    }

    /// <summary>Entfernt einen Chip (einzelner Ort oder kompletter Bezirk) aus der Auswahl</summary>
    [RelayCommand]
    private void RemoveOrtChip(OrtChip chip)
    {
        var toRemove = chip.Orte.ToHashSet();
        ReplaceSelectedOrte(SelectedOrte.Where(o => !toRemove.Contains(o)).ToList());
        OnFiltersChanged();
    }

    /// <summary>
    /// Gruppiert die aktive Ort-Auswahl fuer die Chip-Anzeige: komplett gewaehlte
    /// Bezirke werden zu einem Chip "{Bezirk} (alle)" zusammengefasst, der Rest bleibt einzeln.
    /// </summary>
    private void RebuildOrtChips()
    {
        OrtChips.Clear();
        var remaining = SelectedOrte.ToHashSet();

        foreach (var bezirk in OrtBezirke)
        {
            if (bezirk.Gemeinden.Count == 0 || !bezirk.Gemeinden.All(g => remaining.Contains(g.Name)))
                continue;

            OrtChips.Add(new OrtChip(_loc.BezirkAllFormat(bezirk.Name), bezirk.Gemeinden.Select(g => g.Name).ToList()));
            foreach (var gemeinde in bezirk.Gemeinden)
                remaining.Remove(gemeinde.Name);
        }

        foreach (var ort in SelectedOrte)
        {
            if (remaining.Remove(ort))
                OrtChips.Add(new OrtChip(ort, [ort]));
        }
    }

    /// <summary>
    /// Aktualisiert die Treffer-Vorschau auf dem "Übernehmen"-Button (debounced),
    /// waehrend im Panel Orte an-/abgewaehlt werden.
    /// </summary>
    private void ScheduleOrtCountPreview()
    {
        _ortCountCts?.Cancel();
        _ortCountCts = new CancellationTokenSource();
        _ = UpdateOrtCountPreviewAsync(_ortCountCts.Token);
    }

    private async Task UpdateOrtCountPreviewAsync(CancellationToken token)
    {
        try
        {
            OrtPanelApplyText = _loc.Apply;
            await Task.Delay(400, token);

            var pending = OrtBezirke
                .SelectMany(b => b.Gemeinden)
                .Where(g => g.IsSelected)
                .Select(g => g.Name)
                .ToList();

            var request = await BuildPropertiesRequestAsync(0, 1, pending, token);
            var (_, response) = await _mediator.Request(request, token);
            if (token.IsCancellationRequested || response == null)
                return;

            OrtPanelApplyText = _loc.ApplyWithCountFormat(FormatObjektCount(response.Total));
        }
        catch (OperationCanceledException)
        {
            // Debounce abgebrochen - ignorieren
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[HomePage] Treffer-Vorschau fuer Ort-Panel fehlgeschlagen");
        }
    }

    #endregion

    #region Auth

    private void OnAuthenticationStateChanged(object? sender, bool isAuthenticated)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateAuthState();
            _filterPreferencesLoaded = false;

            // Neu laden wenn sich der Auth-Status aendert (Blockiert-Filter haengt davon ab)
            _ = ReloadPropertiesAsync();

            if (isAuthenticated)
            {
                _ = LoadFilterPreferencesAsync();
            }
        });
    }

    private void UpdateAuthState()
    {
        IsAuthenticated = _authService.IsAuthenticated;
    }

    #endregion

    #region Laden / Pagination

    // Reload-Wunsch waehrend eines laufenden Loads (z.B. Filter-Preferences kommen
    // waehrend des Initial-Loads an) - wird nach dem laufenden Load nachgeholt,
    // sonst zeigt die Liste nicht die restaurierten Filter/Sortierung
    private bool _reloadQueued;

    /// <summary>
    /// Laedt die erste Seite neu (mit Busy-Anzeige)
    /// </summary>
    private async Task ReloadPropertiesAsync()
    {
        if (IsBusy)
        {
            _reloadQueued = true;
            return;
        }

        IsBusy = true;
        BusyMessage = _loc.BusyLoading;
        LoadErrorMessage = null;
        try
        {
            do
            {
                _reloadQueued = false;
                _currentPage = 0;
                var items = await LoadPageAsync(0, CancellationToken.None);
                if (items is not null)
                {
                    ReplaceProperties(items);
                    UpdateResultCount();
                }
            }
            while (_reloadQueued);

            ScrollToTopRequested?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            IsBusy = false;
            BusyMessage = null;
        }
    }

    /// <summary>
    /// Wendet die Aenderungen eines Immobilien-Delta-Syncs auf die sichtbare Liste an.
    /// Geloeschte werden entfernt, geaenderte in-place ersetzt. Nur wenn neue Immobilien
    /// dazugekommen sind, wird die aktuelle Seite einmal frisch geladen - ob eine neue
    /// Immobilie zu den aktiven Filtern passt, entscheidet das Backend.
    /// </summary>
    private void ApplySyncedChanges(PropertyDataSyncedEvent evt)
    {
#if DEBUG
        if (_debugMockProperties != null)
            return;
#endif
        if (evt.FullResync)
        {
            _ = ReloadPropertiesAsync();
            return;
        }

        var removed = 0;
        foreach (var deletedId in evt.DeletedIds)
        {
            var existing = Properties.FirstOrDefault(p => p.Id == deletedId);
            if (existing is not null && Properties.Remove(existing))
                removed++;
        }

        if (removed > 0)
        {
            _totalCount = Math.Max(0, _totalCount - removed);
            UpdateResultCount();
        }

        foreach (var fresh in evt.ChangedProperties)
        {
            for (var i = 0; i < Properties.Count; i++)
            {
                if (Properties[i].Id == fresh.Id)
                {
                    Properties[i] = fresh;
                    break;
                }
            }
        }

        if (evt.CreatedIds.Count > 0)
            _ = SilentRefreshCurrentPageAsync();
    }

    /// <summary>
    /// Laedt die aktuelle Seite still neu (ohne Busy-Overlay), z.B. wenn der Delta-Sync
    /// neue Immobilien gemeldet hat. Laufende Ladevorgaenge haben Vorrang.
    /// </summary>
    private async Task SilentRefreshCurrentPageAsync()
    {
        if (IsBusy || IsRefreshing)
            return;

        try
        {
            var items = await LoadPageAsync(_currentPage, CancellationToken.None, forceRemoteRefresh: true);
            if (items is not null)
            {
                ReplaceProperties(items);
                UpdateResultCount();
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "[HomePage] Stille Aktualisierung nach Sync fehlgeschlagen");
        }
    }

    /// <summary>
    /// Pull-to-Refresh
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            LoadErrorMessage = null;
            _currentPage = 0;
            var items = await LoadPageAsync(0, CancellationToken.None, forceRemoteRefresh: true);
            if (items is not null)
            {
                ReplaceProperties(items);
                UpdateResultCount();
            }
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private Task GoToPreviousPageAsync()
        => LoadRequestedPageAsync(_currentPage - 1);

    [RelayCommand]
    private Task GoToNextPageAsync()
        => LoadRequestedPageAsync(_currentPage + 1);

    private async Task LoadRequestedPageAsync(int page)
    {
        if (IsBusy || IsRefreshing || page < 0 || page >= PageCount || page == _currentPage)
            return;

        IsBusy = true;
        BusyMessage = _loc.BusyLoadingPageFormat(page + 1);
        try
        {
            var items = await LoadPageAsync(page, CancellationToken.None);
            if (items is null || (items.Count == 0 && _totalCount > 0))
                return;

            _currentPage = page;
            ReplaceProperties(items);
            UpdatePaginationState();
            ScrollToTopRequested?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            IsBusy = false;
            BusyMessage = null;
        }
    }

    private void ReplaceProperties(IEnumerable<PropertyListItemDto> items)
        => Properties.ReplaceRange(items);

    /// <summary>
    /// Baut den API-Request mit allen server-seitigen Filtern. Die Ort-Auswahl wird
    /// explizit uebergeben, damit die Treffer-Vorschau im Ort-Panel (Arbeitskopie)
    /// denselben Weg nutzt wie das eigentliche Laden.
    /// </summary>
    private Task<GetPropertiesHttpRequest> BuildPropertiesRequestAsync(
        int page,
        int pageSize,
        IReadOnlyCollection<string> orte,
        CancellationToken cancellationToken = default)
        => PropertyFilterRequestBuilder.BuildAsync(
            new FilterState
            {
                IsHausSelected = IsHausSelected,
                IsGrundstueckSelected = IsGrundstueckSelected,
                IsZwangsversteigerungSelected = IsZwangsversteigerungSelected,
                SelectedAgeFilter = _selectedAgeFilter,
                SelectedOrte = SelectedOrte.ToList(),
                IsPrivateSelected = IsPrivateSelected,
                IsBrokerSelected = IsBrokerSelected,
                ExcludedSellerSourceIds = _excludedSellerSourceIds,
                SelectedSort = _selectedSort
            },
            page,
            pageSize,
            _locationService,
            _logger,
            cancellationToken,
            orte);

    /// <summary>
    /// Laedt eine Seite von der API mit allen server-seitigen Filtern
    /// </summary>
    private async Task<List<PropertyListItemDto>?> LoadPageAsync(
        int page,
        CancellationToken ct,
        bool forceRemoteRefresh = false)
    {
        _logger.LogInformation("[HomePage] Loading page {Page} with pageSize {PageSize}", page, SelectedPageSize);

        // Der sichtbare Stand wird vor dem Request gesichert. Bei einem Fehler darf
        // weder eine leere Rueckgabe noch ein paralleler CollectionView-Reset die
        // bereits nutzbaren Offline-Daten ausblenden.
        var visibleItemsBeforeLoad = page == 0 && Properties.Count > 0
            ? Properties.ToList()
            : null;
        var totalCountBeforeLoad = _totalCount;

        try
        {
#if DEBUG
            if (_debugMockProperties != null)
            {
                IsShowingCachedData = false;
                _totalCount = _debugMockProperties.Count;
                var mockPage = _debugMockProperties
                    .Skip(page * SelectedPageSize)
                    .Take(SelectedPageSize)
                    .ToList();
                _logger.LogInformation("[HomePage] Mock page {Page} returned {Count} of {Total} properties",
                    page, mockPage.Count, _totalCount);
                return mockPage;
            }
#endif
            var request = await BuildPropertiesRequestAsync(page, SelectedPageSize, SelectedOrte.ToList(), ct);
            Action<IMediatorContext>? configure = forceRemoteRefresh
                ? static context => context.ForceCacheRefresh()
                : null;
            var (_, response) = await _mediator.Request(request, ct, configure);
            IsShowingCachedData = _offlineReadState.IsBackendUnavailable;

            _logger.LogInformation("[HomePage] Response received. Properties count: {Count}, HasMore: {HasMore}",
                response?.Properties?.Count ?? 0, response?.HasMore ?? false);

            _totalCount = response?.Total ?? 0;

            return response?.Properties?.ToList() ?? [];
        }
        catch (Exception ex)
        {
            IsShowingCachedData = _offlineReadState.IsBackendUnavailable;
            _logger.LogError(ex, "[HomePage] Error loading page {Page}", page);
            // Kein modaler Dialog: der wuerde (v.a. beim App-Start) alle Eingaben blockieren
            // und das Busy-Overlay bis zum OK-Tap festhaengen. Stattdessen Inline-Fehlerzustand
            // mit Retry-Button; Fehler beim expliziten Seitenwechsel bleiben still.
            if (page == 0)
            {
                // Einen bereits sichtbaren, zuvor erfolgreich geladenen Stand nie
                // durch eine leere Fehlerantwort ersetzen. So bleiben Textdaten bei
                // einem Backend-Ausfall nutzbar; der Offline-Hinweis kennzeichnet
                // sie als zwischengespeichert/veraltet.
                if (visibleItemsBeforeLoad is { Count: > 0 })
                {
                    ReplaceProperties(visibleItemsBeforeLoad);
                    _totalCount = totalCountBeforeLoad;
                    IsEmpty = false;
                    UpdatePaginationState();
                }
                else
                {
                    _totalCount = 0;
                }
                LoadErrorMessage = _loc.LoadErrorFormat(PropertyCollectionViewModelBase.GetErrorHint(ex));
            }
            return null;
        }
    }

    /// <summary>
    /// Erneut versuchen nach fehlgeschlagenem Laden (Inline-Fehlerzustand)
    /// </summary>
    [RelayCommand]
    private Task RetryLoadAsync() => ReloadPropertiesAsync();

    private void UpdateResultCount()
    {
        IsEmpty = _totalCount == 0;
        _filterStateService.SetResultCount(_totalCount);
        UpdatePaginationState();
    }

    private void UpdatePaginationState()
    {
        OnPropertyChanged(nameof(HasResults));
        OnPropertyChanged(nameof(PageCount));
        OnPropertyChanged(nameof(PageNumberText));
        OnPropertyChanged(nameof(HasPagination));
        OnPropertyChanged(nameof(CanGoToPreviousPage));
        OnPropertyChanged(nameof(CanGoToNextPage));
    }

    partial void OnSelectedPageSizeChanged(int value)
    {
        if (_isSyncing)
            return;

        var normalized = PageSizePreference.Normalize(value);
        if (normalized != value)
        {
            _isSyncing = true;
            SelectedPageSize = normalized;
            _isSyncing = false;
        }

        PageSizePreference.Set(normalized);
        _currentPage = 0;
        _ = ReloadPropertiesAsync();
    }

#if DEBUG
    /// <summary>
    /// Debug-only Datensatz fuer reproduzierbare CollectionView-Stresstests. Im
    /// unpaginierten Modus landen alle Eintraege in der CollectionView; mit
    /// Pagination verhaelt sich der Mock wie die serverseitige API.
    /// </summary>
    internal async Task LoadDebugMockPropertiesAsync(int count, bool usePagination)
    {
        count = Math.Clamp(count, 1, 20_000);
        var templates = Properties.Count > 0
            ? Properties.ToList()
            : [CreateFallbackMockProperty()];

        var stopwatch = Stopwatch.StartNew();
        var mockProperties = await Task.Run(() => Enumerable.Range(0, count)
            .Select(index => CloneForMock(templates[index % templates.Count], index))
            .ToList());
        stopwatch.Stop();

        _debugMockProperties = mockProperties;
        _isShowingAllDebugMock = !usePagination;
        _currentPage = 0;
        _totalCount = count;

        if (usePagination)
        {
            await ReloadPropertiesAsync();
        }
        else
        {
            ReplaceProperties(mockProperties);
            UpdateResultCount();
        }

        _logger.LogInformation(
            "[HomePage] Debug mock loaded: {Count} properties, pagination={Pagination}, generation={ElapsedMs}ms",
            count, usePagination, stopwatch.ElapsedMilliseconds);
    }

    internal async Task ClearDebugMockPropertiesAsync()
    {
        _debugMockProperties = null;
        _isShowingAllDebugMock = false;
        await ReloadPropertiesAsync();
    }

    private static PropertyListItemDto CloneForMock(PropertyListItemDto source, int index)
        => new()
        {
            Id = Guid.NewGuid(),
            Title = $"{source.Title} · Mock {index + 1}",
            Address = source.Address,
            MunicipalityId = source.MunicipalityId,
            City = source.City,
            PostalCode = source.PostalCode,
            Price = source.Price + (index % 17) * 1_000,
            LivingAreaM2 = source.LivingAreaM2,
            PlotAreaM2 = source.PlotAreaM2,
            Rooms = source.Rooms,
            Type = source.Type,
            SellerType = source.SellerType,
            SellerName = source.SellerName,
            ImageUrls = source.ImageUrls?.ToList() ?? [],
            CreatedAt = source.CreatedAt.AddMinutes(-index),
            InquiryType = source.InquiryType,
            SourceName = source.SourceName
        };

    private static PropertyListItemDto CreateFallbackMockProperty()
        => new()
        {
            Id = Guid.NewGuid(),
            Title = "Performance-Test Immobilie",
            Address = "Teststraße 20",
            MunicipalityId = Guid.NewGuid(),
            City = "Linz",
            PostalCode = "4020",
            Price = 450_000,
            LivingAreaM2 = 110,
            PlotAreaM2 = 600,
            Rooms = 4,
            Type = PropertyType.House,
            SellerType = SellerType.Private,
            SellerName = "Performance Mock",
            ImageUrls = [],
            CreatedAt = DateTimeOffset.UtcNow,
            InquiryType = default,
            SourceName = "Performance Mock"
        };
#endif

    private string FormatObjektCount(int count)
        => count == 1 ? _loc.ObjektCountOne : _loc.ObjektCountFormat(count);

    #endregion

    #region Commands

    [RelayCommand]
    private Task OpenFilterSettingsAsync()
        => _navigator.NavigateTo<FilterSettingsViewModel>();

    /// <summary>
    /// Oeffnet die Vollbild-Kartenansicht (Karte-Pille). Die aktuellen Filter
    /// nimmt die Karte selbst aus dem FilterStateService mit.
    /// </summary>
    [RelayCommand]
    private Task OpenMapAsync()
        => _navigator.NavigateTo<PropertyMapViewModel>();

    [RelayCommand]
    private void ToggleHaus() => IsHausSelected = !IsHausSelected;

    [RelayCommand]
    private void ToggleGrundstueck() => IsGrundstueckSelected = !IsGrundstueckSelected;

    [RelayCommand]
    private void ToggleZwangsversteigerung() => IsZwangsversteigerungSelected = !IsZwangsversteigerungSelected;

    [RelayCommand]
    private void TogglePrivate() => IsPrivateSelected = !IsPrivateSelected;

    [RelayCommand]
    private void ToggleBroker() => IsBrokerSelected = !IsBrokerSelected;

    [RelayCommand]
    private void OpenSortSheet() => IsSortSheetOpen = true;

    [RelayCommand]
    private void OpenTypeSheet() => IsTypeSheetOpen = true;

    [RelayCommand]
    private void OpenAgeSheet() => IsAgeSheetOpen = true;

    [RelayCommand]
    private void CloseTypeSheet() => IsTypeSheetOpen = false;

    /// <summary>Sortierung aus dem Sheet waehlen (Einzelauswahl schliesst das Sheet)</summary>
    [RelayCommand]
    private void SelectSort(string sortName)
    {
        IsSortSheetOpen = false;

        if (!Enum.TryParse<SortOption>(sortName, out var sort) || sort == _selectedSort)
            return;

        _selectedSort = sort;
        OnFiltersChanged();
    }

    /// <summary>Zeitraum aus dem Sheet waehlen (Einzelauswahl schliesst das Sheet)</summary>
    [RelayCommand]
    private void SelectAgeFilter(int index)
    {
        IsAgeSheetOpen = false;
        SelectedAgeFilterIndex = index;
    }

    private string GetSortLabel(SortOption sort) => sort switch
    {
        SortOption.Aelteste => _loc.SortLabelOldest,
        SortOption.PreisAuf => _loc.SortLabelPriceUp,
        SortOption.PreisAb => _loc.SortLabelPriceDown,
        SortOption.FlaecheAb => _loc.SortLabelAreaDown,
        SortOption.FlaecheAuf => _loc.SortLabelAreaUp,
        SortOption.PlzAuf => _loc.SortLabelPlzUp,
        SortOption.PlzAb => _loc.SortLabelPlzDown,
        _ => _loc.SortLabelNewest
    };

    /// <summary>
    /// Navigiert zur Detail-Seite der ausgewaehlten Immobilie
    /// </summary>
    [RelayCommand]
    private async Task PropertySelectedAsync(PropertyListItemDto property)
    {
        _logger.LogInformation("[HomePage] Navigating to details for {PropertyId}", property.Id);

        // Vor der Navigation: der Detailseite die bereits geladenen Listendaten mitgeben
        _detailPreloader.Prepare(property);

        if (property.Type == PropertyType.Foreclosure)
        {
            await _navigator.NavigateTo<ForeclosureDetailViewModel>(vm => vm.PropertyId = property.Id.ToString());
        }
        else
        {
            await _navigator.NavigateTo<PropertyDetailViewModel>(vm => vm.PropertyId = property.Id.ToString());
        }
    }

    /// <summary>
    /// Favorisiert/Entfavorisiert eine Immobilie
    /// </summary>
    [RelayCommand]
    private async Task ToggleFavoriteAsync(PropertyListItemDto property)
    {
        if (!_authService.IsAuthenticated)
        {
            await _dialogs.Alert(_loc.LoginRequiredTitle, _loc.LoginRequiredFavorite, _commonLoc.Ok);
            return;
        }

        var isFavorite = await _propertyStatusService.ToggleFavoriteAsync(property.Id);
        _logger.LogInformation("[HomePage] Favorite toggled for {PropertyId}: {IsFavorite}", property.Id, isFavorite);
    }

    /// <summary>
    /// Blockiert eine Immobilie (wird aus der Liste ausgeblendet)
    /// </summary>
    [RelayCommand]
    private async Task ToggleBlockedAsync(PropertyListItemDto property)
    {
        if (!_authService.IsAuthenticated)
        {
            await _dialogs.Alert(_loc.LoginRequiredTitle, _loc.LoginRequiredBlock, _commonLoc.Ok);
            return;
        }

        // Keine Rueckfrage: Blockieren ist trivial umkehrbar (Blockiert-Seite)
        var isBlocked = await _propertyStatusService.ToggleBlockedAsync(property.Id);
        if (isBlocked)
        {
            Properties.Remove(property);
            _totalCount = Math.Max(0, _totalCount - 1);
            UpdateResultCount();
        }
    }

    #endregion

    public void Dispose()
    {
        _saveDebounceCts?.Cancel();
        _ortCountCts?.Cancel();
        _syncSubscription?.Dispose();
        _syncSubscription = null;
        _authService.AuthenticationStateChanged -= OnAuthenticationStateChanged;
        _filterStateService.FilterStateChanged -= OnFilterStateChanged;
    }
}
