using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Features.Auth;
using Heimatplatz.Maui.Features.Properties.Models;
using Heimatplatz.Maui.Features.Properties.Services;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Maui.Features.Properties.Presentation;

/// <summary>
/// ViewModel fuer die HomePage (Immobilien-Liste mit Filterleiste,
/// Pull-to-Refresh, Infinite-Scroll-Pagination und Sortierung).
/// Wird als ShellContent "MainPage" eingebunden (registerRoute: false).
/// </summary>
[ShellMap<HomePage>(registerRoute: false)]
public partial class HomeViewModel : ObservableObject, IPageLifecycleAware, IDisposable
{
    private const int PageSize = 20;

    private readonly IAuthService _authService;
    private readonly INavigator _navigator;
    private readonly IDialogs _dialogs;
    private readonly IFilterPreferencesService _filterPreferencesService;
    private readonly IFilterStateService _filterStateService;
    private readonly IPropertyStatusService _propertyStatusService;
    private readonly ILocationService _locationService;
    private readonly IMediator _mediator;
    private readonly ILogger<HomeViewModel> _logger;

    private int _currentPage;
    private int _totalCount;
    private bool _hasMore;
    private SortOption _selectedSort = SortOption.Neueste;
    private AgeFilter _selectedAgeFilter = AgeFilter.Alle;
    private List<LocationGemeindeDto> _municipalities = [];
    private bool _isSyncing;
    private bool _suppressSearch;
    private CancellationTokenSource? _saveDebounceCts;

    public ObservableCollection<PropertyListItemDto> Properties { get; } = [];

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string? BusyMessage { get; set; }

    [ObservableProperty]
    public partial bool IsRefreshing { get; set; }

    [ObservableProperty]
    public partial bool IsLoadingMore { get; set; }

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial string ResultCountText { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilterToggleGlyph))]
    public partial bool IsFilterExpanded { get; set; }

    /// <summary>
    /// Chevron-Glyph fuer den Filter-Toggle-Button
    /// </summary>
    public string FilterToggleGlyph => IsFilterExpanded ? "▲" : "▼";

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
    public IReadOnlyList<string> AgeFilterOptions { get; } = ["Alle", "1 Tag", "1 Woche", "1 Monat", "1 Jahr"];

    [ObservableProperty]
    public partial int SelectedAgeFilterIndex { get; set; }

    /// <summary>
    /// Kurze Zusammenfassung der aktiven Filter fuer den eingeklappten Filter-Header
    /// </summary>
    [ObservableProperty]
    public partial string FilterSummary { get; set; }

    [ObservableProperty]
    public partial string SortLabel { get; set; }

    // Ort-Auswahl (inline in der Filterleiste)
    public ObservableCollection<string> SelectedOrte { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedOrte))]
    public partial int SelectedOrteCount { get; set; }

    public bool HasSelectedOrte => SelectedOrteCount > 0;

    [ObservableProperty]
    public partial string OrtSearchText { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasOrtSuggestions))]
    public partial List<LocationGemeindeDto> OrtSuggestions { get; set; }

    public bool HasOrtSuggestions => OrtSuggestions.Count > 0;

    public HomeViewModel(
        IAuthService authService,
        INavigator navigator,
        IDialogs dialogs,
        IFilterPreferencesService filterPreferencesService,
        IFilterStateService filterStateService,
        IPropertyStatusService propertyStatusService,
        ILocationService locationService,
        IMediator mediator,
        ILogger<HomeViewModel> logger)
    {
        _authService = authService;
        _navigator = navigator;
        _dialogs = dialogs;
        _filterPreferencesService = filterPreferencesService;
        _filterStateService = filterStateService;
        _propertyStatusService = propertyStatusService;
        _locationService = locationService;
        _mediator = mediator;
        _logger = logger;

        // Initialwerte (partial properties koennen keine Initializer haben)
        _isSyncing = true;
        IsHausSelected = true;
        IsGrundstueckSelected = true;
        IsZwangsversteigerungSelected = true;
        IsPrivateSelected = true;
        IsBrokerSelected = true;
        SelectedAgeFilterIndex = 0;
        ResultCountText = "0 Objekte";
        SortLabel = "Neueste";
        OrtSearchText = string.Empty;
        OrtSuggestions = [];
        FilterSummary = string.Empty;
        _isSyncing = false;

        UpdateFilterSummary();

        _authService.AuthenticationStateChanged += OnAuthenticationStateChanged;
        _filterStateService.FilterStateChanged += OnFilterStateChanged;

        UpdateAuthState();

        // Gemeinden fuer Namens->Id-Mapping laden
        _ = LoadMunicipalitiesAsync();

        // Gespeicherte Filter laden wenn bereits angemeldet
        if (_authService.IsAuthenticated)
        {
            _ = LoadFilterPreferencesAsync();
        }
    }

    #region IPageLifecycleAware

    public void OnAppearing()
    {
        // Session-Filter-State wiederherstellen (z.B. nach Rueckkehr von einer Detail-Seite)
        SyncFiltersFromService();

        if (Properties.Count == 0 && !IsBusy)
        {
            _ = ReloadPropertiesAsync();
        }

        if (_authService.IsAuthenticated)
        {
            _ = _propertyStatusService.EnsureLoadedAsync();
        }
    }

    public void OnDisappearing()
    {
    }

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
            _selectedSort = state.SelectedSort;
            SortLabel = GetSortLabel(_selectedSort);
            UpdateFilterSummary();
        }
        finally
        {
            _isSyncing = false;
        }
    }

    private Task? _municipalitiesLoadTask;

    /// <summary>
    /// Laedt die Gemeinden genau einmal (single-flight). Nach einem Fehlschlag wird
    /// beim naechsten Aufruf erneut versucht.
    /// </summary>
    private Task LoadMunicipalitiesAsync()
    {
        if (_municipalities.Count > 0)
            return Task.CompletedTask;

        return _municipalitiesLoadTask ??= LoadMunicipalitiesCoreAsync();

        async Task LoadMunicipalitiesCoreAsync()
        {
            try
            {
                _municipalities = await _locationService.GetAllMunicipalitiesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[HomePage] Failed to load locations from API");
                _municipalitiesLoadTask = null; // beim naechsten Bedarf erneut versuchen
            }
        }
    }

    private async Task LoadFilterPreferencesAsync()
    {
        try
        {
            var preferences = await _filterPreferencesService.GetPreferencesAsync();
            if (preferences != null)
            {
                ApplyFilterPreferences(preferences);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[HomePage] Failed to load filter preferences");
        }
    }

    private void ApplyFilterPreferences(FilterPreferencesDto preferences)
    {
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
            _selectedSort = preferences.SelectedSort;
            SortLabel = GetSortLabel(_selectedSort);
            UpdateFilterSummary();
        }
        finally
        {
            _isSyncing = false;
        }

        // Mit neuen Filtern neu laden (server-seitig)
        _ = ReloadPropertiesAsync();
    }

    private void ReplaceSelectedOrte(IEnumerable<string> orte)
    {
        SelectedOrte.Clear();
        foreach (var ort in orte)
            SelectedOrte.Add(ort);
        SelectedOrteCount = SelectedOrte.Count;
    }

    /// <summary>
    /// Baut die Kurzform der aktiven Filter fuer den eingeklappten Header,
    /// z.B. "Haus, Grundstück · Privat · 1 Woche · 2 Orte".
    /// </summary>
    private void UpdateFilterSummary()
    {
        var parts = new List<string>();

        var types = new List<string>();
        if (IsHausSelected) types.Add("Haus");
        if (IsGrundstueckSelected) types.Add("Grundstück");
        if (IsZwangsversteigerungSelected) types.Add("Zwangsversteigerung");
        parts.Add(types.Count == 3 ? "Alle Typen" : string.Join(", ", types));

        if (IsSellerFilterVisible && IsPrivateSelected != IsBrokerSelected)
            parts.Add(IsPrivateSelected ? "Privat" : "Makler");

        if (_selectedAgeFilter != AgeFilter.Alle)
            parts.Add(AgeFilterOptions[(int)_selectedAgeFilter]);

        parts.Add(SelectedOrte.Count == 0
            ? "Alle Orte"
            : SelectedOrte.Count == 1 ? SelectedOrte[0] : $"{SelectedOrte.Count} Orte");

        FilterSummary = string.Join(" · ", parts);
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
                ExcludedSellerSourceIds: [],
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

    #region Ort-Auswahl

    partial void OnOrtSearchTextChanged(string value)
    {
        if (_suppressSearch) return;

        if (string.IsNullOrWhiteSpace(value) || value.Length < 2)
        {
            OrtSuggestions = [];
            return;
        }

        var search = value.Trim();
        OrtSuggestions = _municipalities
            .Where(m => (m.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                      || m.PostalCode.StartsWith(search, StringComparison.OrdinalIgnoreCase))
                     && !SelectedOrte.Contains(m.Name))
            .Take(15)
            .ToList();
    }

    /// <summary>
    /// Fuegt einen Ort zur Auswahl hinzu
    /// </summary>
    [RelayCommand]
    private void AddOrt(LocationGemeindeDto gemeinde)
    {
        if (!SelectedOrte.Contains(gemeinde.Name))
        {
            SelectedOrte.Add(gemeinde.Name);
            SelectedOrteCount = SelectedOrte.Count;
        }

        _suppressSearch = true;
        OrtSearchText = string.Empty;
        _suppressSearch = false;
        OrtSuggestions = [];

        OnFiltersChanged();
    }

    /// <summary>
    /// Entfernt einen Ort aus der Auswahl
    /// </summary>
    [RelayCommand]
    private void RemoveOrt(string ort)
    {
        if (SelectedOrte.Remove(ort))
        {
            SelectedOrteCount = SelectedOrte.Count;
            OnFiltersChanged();
        }
    }

    #endregion

    #region Auth

    private void OnAuthenticationStateChanged(object? sender, bool isAuthenticated)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateAuthState();

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
        BusyMessage = "Lade Immobilien...";
        try
        {
            do
            {
                _reloadQueued = false;
                _currentPage = 0;
                var items = await LoadPageAsync(0, CancellationToken.None);

                Properties.Clear();
                foreach (var item in items)
                    Properties.Add(item);

                UpdateResultCount();
            }
            while (_reloadQueued);
        }
        finally
        {
            IsBusy = false;
            BusyMessage = null;
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
            _currentPage = 0;
            var items = await LoadPageAsync(0, CancellationToken.None);

            Properties.Clear();
            foreach (var item in items)
                Properties.Add(item);

            UpdateResultCount();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    /// <summary>
    /// Naechste Seite laden (Infinite Scroll via RemainingItemsThreshold)
    /// </summary>
    [RelayCommand]
    private async Task LoadMoreAsync()
    {
        if (IsLoadingMore || IsBusy || IsRefreshing || !_hasMore)
            return;

        IsLoadingMore = true;
        try
        {
            var items = await LoadPageAsync(_currentPage + 1, CancellationToken.None);
            if (items.Count > 0)
            {
                _currentPage++;
                foreach (var item in items)
                    Properties.Add(item);
            }
        }
        finally
        {
            IsLoadingMore = false;
        }
    }

    /// <summary>
    /// Laedt eine Seite von der API mit allen server-seitigen Filtern
    /// </summary>
    private async Task<List<PropertyListItemDto>> LoadPageAsync(int page, CancellationToken ct)
    {
        _logger.LogInformation("[HomePage] Loading page {Page} with pageSize {PageSize}", page, PageSize);

        try
        {
            // SortOption auf API-Parameter mappen
            var (sortBy, sortDesc) = _selectedSort switch
            {
                SortOption.Aelteste => ("CreatedAt", false),
                SortOption.PreisAuf => ("Price", false),
                SortOption.PreisAb => ("Price", true),
                SortOption.FlaecheAb => ("PlotArea", true),
                SortOption.FlaecheAuf => ("PlotArea", false),
                SortOption.PlzAuf => ("PostalCode", false),
                SortOption.PlzAb => ("PostalCode", true),
                _ => ((string?)null, true)
            };

            var request = new GetPropertiesHttpRequest
            {
                Page = page,
                PageSize = PageSize,
                SortBy = sortBy,
                SortDescending = sortDesc
            };

            // PropertyTypes-Filter (Multi-Select als JSON-Array)
            var selectedPropertyTypes = new List<string>();
            if (IsHausSelected) selectedPropertyTypes.Add("House");
            if (IsGrundstueckSelected) selectedPropertyTypes.Add("Land");
            if (IsZwangsversteigerungSelected) selectedPropertyTypes.Add("Foreclosure");
            if (selectedPropertyTypes.Count > 0 && selectedPropertyTypes.Count < 3)
            {
                request.PropertyTypesJson = JsonSerializer.Serialize(selectedPropertyTypes);
            }

            // SellerTypes-Filter (Multi-Select als JSON-Array)
            var selectedSellerTypes = new List<string>();
            if (IsPrivateSelected) selectedSellerTypes.Add("Private");
            if (IsBrokerSelected) selectedSellerTypes.Add("Broker");
            if (selectedSellerTypes.Count > 0 && selectedSellerTypes.Count < 2)
            {
                request.SellerTypesJson = JsonSerializer.Serialize(selectedSellerTypes);
            }

            // MunicipalityIds-Filter (Ortsnamen -> Ids). Gemeinden bei Bedarf nachladen,
            // sonst wuerde der Ort-Filter beim Kaltstart (Race mit dem Gemeinden-Load)
            // still ignoriert und die Liste zeigt trotz Filter alle Objekte.
            if (SelectedOrte.Count > 0)
            {
                await LoadMunicipalitiesAsync();

                var ids = _municipalities
                    .Where(m => SelectedOrte.Contains(m.Name))
                    .Select(m => m.Id)
                    .ToList();
                if (ids.Count > 0)
                {
                    request.MunicipalityIdsJson = JsonSerializer.Serialize(ids);
                }
                else
                {
                    _logger.LogWarning("[HomePage] Ort-Filter aktiv, aber keine Gemeinde-Ids aufloesbar ({Orte})",
                        string.Join(", ", SelectedOrte));
                }
            }

            // CreatedAfter-Filter (Alters-Filter)
            if (_selectedAgeFilter != AgeFilter.Alle)
            {
                request.CreatedAfter = _selectedAgeFilter switch
                {
                    AgeFilter.EinTag => DateTimeOffset.UtcNow.AddDays(-1),
                    AgeFilter.EineWoche => DateTimeOffset.UtcNow.AddDays(-7),
                    AgeFilter.EinMonat => DateTimeOffset.UtcNow.AddMonths(-1),
                    AgeFilter.EinJahr => DateTimeOffset.UtcNow.AddYears(-1),
                    _ => DateTimeOffset.MinValue
                };
            }

            var (_, response) = await _mediator.Request(request, ct);

            _logger.LogInformation("[HomePage] Response received. Properties count: {Count}, HasMore: {HasMore}",
                response?.Properties?.Count ?? 0, response?.HasMore ?? false);

            _hasMore = response?.HasMore ?? false;
            _totalCount = response?.Total ?? 0;

            return response?.Properties?.ToList() ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[HomePage] Error loading page {Page}", page);
            _hasMore = false;
            if (page == 0) _totalCount = 0;
            await _dialogs.Alert(
                "Fehler beim Laden",
                "Die Immobilien konnten nicht geladen werden. Bitte überprüfen Sie Ihre Internetverbindung und versuchen Sie es erneut.");
            return [];
        }
    }

    private void UpdateResultCount()
    {
        IsEmpty = _totalCount == 0;
        ResultCountText = $"{_totalCount} Objekte";
        _filterStateService.SetResultCount(_totalCount);
    }

    #endregion

    #region Commands

    [RelayCommand]
    private void ToggleFilterExpanded()
    {
        IsFilterExpanded = !IsFilterExpanded;
    }

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

    /// <summary>
    /// Zeigt die Sortieroptionen als ActionSheet
    /// </summary>
    [RelayCommand]
    private async Task ShowSortOptionsAsync()
    {
        var choice = await _dialogs.ActionSheet(
            "Sortierung",
            "Abbrechen",
            null,
            "Neueste", "Älteste", "Preis ↑", "Preis ↓", "Fläche ↓", "Fläche ↑", "PLZ ↑", "PLZ ↓");

        var newSort = choice switch
        {
            "Neueste" => SortOption.Neueste,
            "Älteste" => SortOption.Aelteste,
            "Preis ↑" => SortOption.PreisAuf,
            "Preis ↓" => SortOption.PreisAb,
            "Fläche ↓" => SortOption.FlaecheAb,
            "Fläche ↑" => SortOption.FlaecheAuf,
            "PLZ ↑" => SortOption.PlzAuf,
            "PLZ ↓" => SortOption.PlzAb,
            _ => (SortOption?)null
        };

        if (newSort == null || newSort == _selectedSort)
            return;

        _selectedSort = newSort.Value;
        SortLabel = GetSortLabel(_selectedSort);
        OnFiltersChanged();
    }

    private static string GetSortLabel(SortOption sort) => sort switch
    {
        SortOption.Aelteste => "Älteste",
        SortOption.PreisAuf => "Preis ↑",
        SortOption.PreisAb => "Preis ↓",
        SortOption.FlaecheAb => "Fläche ↓",
        SortOption.FlaecheAuf => "Fläche ↑",
        SortOption.PlzAuf => "PLZ ↑",
        SortOption.PlzAb => "PLZ ↓",
        _ => "Neueste"
    };

    /// <summary>
    /// Navigiert zur Detail-Seite der ausgewaehlten Immobilie
    /// </summary>
    [RelayCommand]
    private async Task PropertySelectedAsync(PropertyListItemDto property)
    {
        _logger.LogInformation("[HomePage] Navigating to details for {PropertyId}", property.Id);

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
            await _dialogs.Alert("Anmeldung erforderlich", "Bitte melden Sie sich an, um Immobilien zu favorisieren.");
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
            await _dialogs.Alert("Anmeldung erforderlich", "Bitte melden Sie sich an, um Immobilien zu blockieren.");
            return;
        }

        var confirmed = await _dialogs.Confirm(
            "Blockieren?",
            $"Möchten Sie \"{property.Title}\" wirklich blockieren? Die Immobilie wird aus der Liste ausgeblendet.");
        if (!confirmed) return;

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
        _authService.AuthenticationStateChanged -= OnAuthenticationStateChanged;
        _filterStateService.FilterStateChanged -= OnFilterStateChanged;
    }
}
