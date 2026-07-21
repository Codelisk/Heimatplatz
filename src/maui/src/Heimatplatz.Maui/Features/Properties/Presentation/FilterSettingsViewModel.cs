using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Maui.Features.Auth;
using Heimatplatz.Maui.Features.Properties.Models;
using Heimatplatz.Maui.Features.Properties.Services;
using Heimatplatz.Maui.Localization.Properties;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Maui.Features.Properties.Presentation;

/// <summary>
/// Verwaltet die Suchfilter auf einer eigenen Seite. Der FilterStateService ist die
/// gemeinsame Quelle fuer diese Seite und die Ergebnisliste auf der HomePage.
/// </summary>
[ShellMap<FilterSettingsPage>("FilterSettings")]
public partial class FilterSettingsViewModel : ObservableObject, IPageLifecycleAware, IDisposable
{
    private readonly IAuthService _authService;
    private readonly IFilterPreferencesService _filterPreferencesService;
    private readonly IFilterStateService _filterStateService;
    private readonly ILocationService _locationService;
    private readonly IMediator _mediator;
    private readonly ILogger<FilterSettingsViewModel> _logger;

    private bool _isSyncing;
    private bool _preferencesLoaded;
    private Task? _preferencesLoadTask;
    private Task? _ortTreeLoadTask;
    private CancellationTokenSource? _saveDebounceCts;
    private CancellationTokenSource? _resultCountRefreshCts;
    private AgeFilter _selectedAgeFilter = AgeFilter.Alle;
    private SortOption _selectedSort = SortOption.Neueste;
    private List<Guid> _excludedSellerSourceIds = [];

    public FilterSettingsViewModel(
        IAuthService authService,
        IFilterPreferencesService filterPreferencesService,
        IFilterStateService filterStateService,
        ILocationService locationService,
        IMediator mediator,
        ILogger<FilterSettingsViewModel> logger,
        FilterSettingsStringsLocalized loc)
    {
        _authService = authService;
        _filterPreferencesService = filterPreferencesService;
        _filterStateService = filterStateService;
        _locationService = locationService;
        _mediator = mediator;
        _logger = logger;
        Loc = loc;

        AgeFilterOptions = [loc.AgeOptionAll, loc.AgeOptionDay, loc.AgeOptionWeek, loc.AgeOptionMonth, loc.AgeOptionYear];

        _isSyncing = true;
        IsHausSelected = true;
        IsGrundstueckSelected = true;
        IsZwangsversteigerungSelected = false;
        IsPrivateSelected = true;
        IsBrokerSelected = true;
        SelectedAgeFilterIndex = 0;
        SelectedPageSize = PageSizePreference.Get();
        FilterSummary = string.Empty;
        ResultCountText = loc.ObjektCountFormat(0);
        OrtPanelSearchText = string.Empty;
        OrtPanelSearchResults = [];
        OrtPanelApplyText = loc.Apply;
        _isSyncing = false;

        _filterStateService.FilterStateChanged += OnFilterStateChanged;
        _filterStateService.ResultCountChanged += OnResultCountChanged;
        _authService.AuthenticationStateChanged += OnAuthenticationStateChanged;

        SyncFiltersFromService();
        UpdateResultCount();
        ScheduleResultCountRefresh(TimeSpan.Zero);
    }

    /// <summary>Lokalisierte Texte fuer XAML-Bindings (Loc.Key)</summary>
    public FilterSettingsStringsLocalized Loc { get; }

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

    public bool IsSellerFilterVisible
        => !(IsZwangsversteigerungSelected && !IsHausSelected && !IsGrundstueckSelected);

    public IReadOnlyList<string> AgeFilterOptions { get; }

    [ObservableProperty]
    public partial int SelectedAgeFilterIndex { get; set; }

    /// <summary>
    /// "Objekte pro Seite" ist eine lokale Anzeige-Einstellung (kein Server-Filter).
    /// Die HomePage uebernimmt den Wert beim Zurueckkehren aus den Preferences.
    /// </summary>
    public IReadOnlyList<int> PageSizeOptions => PageSizePreference.Options;

    [ObservableProperty]
    public partial int SelectedPageSize { get; set; }

    [ObservableProperty]
    public partial string FilterSummary { get; set; }

    [ObservableProperty]
    public partial string ResultCountText { get; set; }

    public ObservableCollection<string> SelectedOrte { get; } = [];
    public ObservableCollection<OrtChip> OrtChips { get; } = [];
    public ObservableCollection<OrtBezirkItem> OrtBezirke { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedOrte))]
    [NotifyPropertyChangedFor(nameof(OrtFieldLabel))]
    public partial int SelectedOrteCount { get; set; }

    public bool HasSelectedOrte => SelectedOrteCount > 0;

    public string OrtFieldLabel => SelectedOrteCount switch
    {
        0 => Loc.OrtFieldNone,
        1 => SelectedOrte[0],
        _ => Loc.OrtFieldManyFormat(SelectedOrteCount)
    };

    [ObservableProperty]
    public partial bool IsOrtPanelOpen { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOrtPanelSearchActive))]
    [NotifyPropertyChangedFor(nameof(IsOrtPanelBrowseVisible))]
    public partial string OrtPanelSearchText { get; set; }

    [ObservableProperty]
    public partial List<OrtGemeindeItem> OrtPanelSearchResults { get; set; }

    [ObservableProperty]
    public partial string OrtPanelApplyText { get; set; }

    public bool IsOrtPanelSearchActive => OrtPanelSearchText.Trim().Length >= 2;
    public bool IsOrtPanelBrowseVisible => !IsOrtPanelSearchActive;

    public void OnAppearing()
    {
        SyncFiltersFromService();
        UpdateResultCount();

        if (_authService.IsAuthenticated && !_filterStateService.HasSessionState)
            _ = LoadFilterPreferencesAsync();
    }

    public void OnDisappearing()
    {
    }

    private void OnFilterStateChanged(object? sender, EventArgs e)
        => MainThread.BeginInvokeOnMainThread(() =>
        {
            SyncFiltersFromService();
            ScheduleResultCountRefresh();
        });

    private void OnResultCountChanged(object? sender, EventArgs e)
        => MainThread.BeginInvokeOnMainThread(UpdateResultCount);

    private void OnAuthenticationStateChanged(object? sender, bool isAuthenticated)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _preferencesLoaded = false;
            if (isAuthenticated)
                _ = LoadFilterPreferencesAsync();
            else
                _saveDebounceCts?.Cancel();
        });
    }

    private void SyncFiltersFromService()
    {
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
            _selectedSort = state.SelectedSort;
            _excludedSellerSourceIds = state.ExcludedSellerSourceIds.ToList();
            ReplaceSelectedOrte(state.SelectedOrte);
            UpdateFilterSummary();
        }
        finally
        {
            _isSyncing = false;
        }
    }

    private void UpdateResultCount()
    {
        var count = _filterStateService.CurrentState.ResultCount;
        ResultCountText = count == 1 ? Loc.ObjektCountOne : Loc.ObjektCountFormat(count);
    }

    private Task LoadFilterPreferencesAsync()
    {
        if (_preferencesLoaded)
            return Task.CompletedTask;

        return _preferencesLoadTask ??= LoadFilterPreferencesCoreAsync();
    }

    private async Task LoadFilterPreferencesCoreAsync()
    {
        try
        {
            var preferences = await _filterPreferencesService.GetPreferencesAsync();
            _preferencesLoaded = true;
            if (preferences is null)
                return;

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
                _selectedSort = preferences.SelectedSort;
                _excludedSellerSourceIds = preferences.ExcludedSellerSourceIds.ToList();
                ReplaceSelectedOrte(preferences.SelectedOrte);
            }
            finally
            {
                _isSyncing = false;
            }

            PublishFilters(savePreferences: false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[FilterSettingsPage] Gespeicherte Filter konnten nicht geladen werden");
        }
        finally
        {
            _preferencesLoadTask = null;
        }
    }

    partial void OnIsHausSelectedChanged(bool value)
    {
        if (_isSyncing)
            return;

        if (!value && !IsGrundstueckSelected && !IsZwangsversteigerungSelected)
        {
            RestoreSelection(() => IsHausSelected = true);
            return;
        }

        ResetSellerFilterIfOnlyForeclosureSelected();
        PublishFilters();
    }

    partial void OnIsGrundstueckSelectedChanged(bool value)
    {
        if (_isSyncing)
            return;

        if (!value && !IsHausSelected && !IsZwangsversteigerungSelected)
        {
            RestoreSelection(() => IsGrundstueckSelected = true);
            return;
        }

        ResetSellerFilterIfOnlyForeclosureSelected();
        PublishFilters();
    }

    partial void OnIsZwangsversteigerungSelectedChanged(bool value)
    {
        if (_isSyncing)
            return;

        if (!value && !IsHausSelected && !IsGrundstueckSelected)
        {
            RestoreSelection(() => IsZwangsversteigerungSelected = true);
            return;
        }

        ResetSellerFilterIfOnlyForeclosureSelected();
        PublishFilters();
    }

    partial void OnIsPrivateSelectedChanged(bool value)
    {
        if (_isSyncing)
            return;

        if (!value && !IsBrokerSelected)
        {
            RestoreSelection(() => IsPrivateSelected = true);
            return;
        }

        PublishFilters();
    }

    partial void OnIsBrokerSelectedChanged(bool value)
    {
        if (_isSyncing)
            return;

        if (!value && !IsPrivateSelected)
        {
            RestoreSelection(() => IsBrokerSelected = true);
            return;
        }

        PublishFilters();
    }

    partial void OnSelectedAgeFilterIndexChanged(int value)
    {
        if (_isSyncing)
            return;

        _selectedAgeFilter = value >= 0 && value <= (int)AgeFilter.EinJahr
            ? (AgeFilter)value
            : AgeFilter.Alle;
        PublishFilters();
    }

    partial void OnSelectedPageSizeChanged(int value)
    {
        if (_isSyncing)
            return;

        PageSizePreference.Set(value);
    }

    private void RestoreSelection(Action restore)
    {
        _isSyncing = true;
        restore();
        _isSyncing = false;
    }

    private void ResetSellerFilterIfOnlyForeclosureSelected()
    {
        if (IsSellerFilterVisible)
            return;

        _isSyncing = true;
        IsPrivateSelected = true;
        IsBrokerSelected = true;
        _isSyncing = false;
    }

    private void PublishFilters(bool savePreferences = true)
    {
        if (_isSyncing)
            return;

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
                _selectedSort);
        }
        finally
        {
            _filterStateService.FilterStateChanged += OnFilterStateChanged;
        }

        UpdateFilterSummary();
        ScheduleResultCountRefresh();
        if (savePreferences)
            ScheduleAutoSave();
    }

    /// <summary>
    /// Laedt die Trefferzahl direkt mit demselben Request-Builder wie die HomePage.
    /// Die HomePage kann beim Navigieren auf diese separate Shell-Seite bereits disposed
    /// sein und darf daher nicht als indirekter Zaehler-Lieferant vorausgesetzt werden.
    /// </summary>
    private void ScheduleResultCountRefresh(TimeSpan? delay = null)
    {
        _resultCountRefreshCts?.Cancel();
        _resultCountRefreshCts = new CancellationTokenSource();
        _ = RefreshResultCountAsync(delay ?? TimeSpan.FromMilliseconds(250), _resultCountRefreshCts.Token);
    }

    private async Task RefreshResultCountAsync(TimeSpan delay, CancellationToken token)
    {
        try
        {
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, token);

            var request = await PropertyFilterRequestBuilder.BuildAsync(
                _filterStateService.CurrentState,
                page: 0,
                pageSize: 1,
                _locationService,
                _logger,
                token);
            var (_, response) = await _mediator.Request(request, token);

            if (!token.IsCancellationRequested && response != null)
                _filterStateService.SetResultCount(response.Total);
        }
        catch (OperationCanceledException)
        {
            // Eine neuere Filterauswahl hat diese Vorschau ersetzt.
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[FilterSettingsPage] Trefferzahl konnte nicht aktualisiert werden");
        }
    }

    private void ScheduleAutoSave()
    {
        if (!_authService.IsAuthenticated)
            return;

        _saveDebounceCts?.Cancel();
        _saveDebounceCts = new CancellationTokenSource();
        _ = AutoSaveAfterDelayAsync(_saveDebounceCts.Token);
    }

    private async Task AutoSaveAfterDelayAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(800, token);
            var preferences = new FilterPreferencesDto(
                SelectedOrte.ToList(),
                _selectedAgeFilter,
                IsHausSelected,
                IsGrundstueckSelected,
                IsZwangsversteigerungSelected,
                IsPrivateSelected,
                IsBrokerSelected,
                _excludedSellerSourceIds,
                _selectedSort);
            await _filterPreferencesService.SavePreferencesAsync(preferences, token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[FilterSettingsPage] Filter konnten nicht gespeichert werden");
        }
    }

    private void UpdateFilterSummary()
    {
        var parts = new List<string>();
        var types = new List<string>();
        if (IsHausSelected) types.Add(Loc.TypeHouse);
        if (IsGrundstueckSelected) types.Add(Loc.TypeLand);
        if (IsZwangsversteigerungSelected) types.Add(Loc.TypeForeclosure);
        parts.Add(types.Count == 3 ? Loc.AllTypes : string.Join(", ", types));

        if (IsSellerFilterVisible && IsPrivateSelected != IsBrokerSelected)
            parts.Add(IsPrivateSelected ? Loc.SellerPrivate : Loc.SellerBroker);
        if (_selectedAgeFilter != AgeFilter.Alle)
            parts.Add(AgeFilterOptions[(int)_selectedAgeFilter]);
        parts.Add(SelectedOrteCount == 0
            ? Loc.AllOrte
            : SelectedOrteCount == 1 ? SelectedOrte[0] : Loc.OrteCountFormat(SelectedOrteCount));

        FilterSummary = string.Join(" · ", parts);
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

    [RelayCommand]
    private void SelectAgeFilter(int index) => SelectedAgeFilterIndex = index;

    [RelayCommand]
    private void ResetFilters()
    {
        _isSyncing = true;
        IsHausSelected = true;
        IsGrundstueckSelected = true;
        IsZwangsversteigerungSelected = false;
        IsPrivateSelected = true;
        IsBrokerSelected = true;
        _selectedAgeFilter = AgeFilter.Alle;
        SelectedAgeFilterIndex = (int)AgeFilter.Alle;
        ReplaceSelectedOrte([]);
        _isSyncing = false;
        PublishFilters();
    }

    [RelayCommand]
    private async Task OpenOrtPanelAsync()
    {
        await EnsureOrtTreeAsync();
        OrtPanelSearchText = string.Empty;
        OrtPanelSearchResults = [];
        SyncOrtPanelFromSelection();
        UpdateOrtPanelApplyText();
        IsOrtPanelOpen = true;
    }

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
                    CountLabelFormatter = count => Loc.SelectedCountFormat(count)
                };
                foreach (var gemeinde in gemeinden)
                    gemeinde.Bezirk = bezirk;
                return bezirk;
            });

        OrtBezirke.Clear();
        foreach (var bezirk in bezirke)
            OrtBezirke.Add(bezirk);
        RebuildOrtChips();
    }

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

    [RelayCommand]
    private void ToggleBezirkSelection(OrtBezirkItem bezirk)
    {
        var target = !bezirk.IsAllSelected;
        foreach (var gemeinde in bezirk.Gemeinden)
            gemeinde.IsSelected = target;
        bezirk.RefreshSelectedCount();
        UpdateOrtPanelApplyText();
    }

    [RelayCommand]
    private void ToggleOrtGemeinde(OrtGemeindeItem gemeinde)
    {
        gemeinde.IsSelected = !gemeinde.IsSelected;
        gemeinde.Bezirk?.RefreshSelectedCount();
        UpdateOrtPanelApplyText();
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
        UpdateOrtPanelApplyText();
    }

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
        PublishFilters();
    }

    [RelayCommand]
    private void RemoveOrtChip(OrtChip chip)
    {
        var toRemove = chip.Orte.ToHashSet();
        ReplaceSelectedOrte(SelectedOrte.Where(o => !toRemove.Contains(o)).ToList());
        PublishFilters();
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

    private void RebuildOrtChips()
    {
        OrtChips.Clear();
        var remaining = SelectedOrte.ToHashSet();

        foreach (var bezirk in OrtBezirke)
        {
            if (bezirk.Gemeinden.Count == 0 || !bezirk.Gemeinden.All(g => remaining.Contains(g.Name)))
                continue;

            OrtChips.Add(new OrtChip(Loc.BezirkAllFormat(bezirk.Name), bezirk.Gemeinden.Select(g => g.Name).ToList()));
            foreach (var gemeinde in bezirk.Gemeinden)
                remaining.Remove(gemeinde.Name);
        }

        foreach (var ort in SelectedOrte)
        {
            if (remaining.Remove(ort))
                OrtChips.Add(new OrtChip(ort, [ort]));
        }
    }

    private void UpdateOrtPanelApplyText()
    {
        var count = OrtBezirke.Sum(b => b.Gemeinden.Count(g => g.IsSelected));
        OrtPanelApplyText = count switch
        {
            0 => Loc.ApplyAllOrte,
            1 => Loc.ApplyOneOrt,
            _ => Loc.ApplyManyOrteFormat(count)
        };
    }

    public void Dispose()
    {
        _saveDebounceCts?.Cancel();
        _resultCountRefreshCts?.Cancel();
        _filterStateService.FilterStateChanged -= OnFilterStateChanged;
        _filterStateService.ResultCountChanged -= OnResultCountChanged;
        _authService.AuthenticationStateChanged -= OnAuthenticationStateChanged;
    }
}
