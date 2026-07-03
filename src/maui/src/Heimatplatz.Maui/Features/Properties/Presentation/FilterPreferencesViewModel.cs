using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Maui.Features.Properties.Models;
using Heimatplatz.Maui.Features.Properties.Services;
using Microsoft.Extensions.Logging;
using Shiny;

namespace Heimatplatz.Maui.Features.Properties.Presentation;

/// <summary>
/// ViewModel fuer die FilterPreferencesPage (gespeicherte Filter).
/// Aenderungen an Filtern werden automatisch nach kurzer Verzoegerung gespeichert (Debounce)
/// und in den FilterStateService synchronisiert, damit die HomePage sie uebernimmt.
/// </summary>
[ShellMap<FilterPreferencesPage>("FilterPreferences")]
public partial class FilterPreferencesViewModel : ObservableObject, IPageLifecycleAware
{
    private readonly IFilterPreferencesService _filterPreferencesService;
    private readonly IFilterStateService _filterStateService;
    private readonly ILocationService _locationService;
    private readonly ILogger<FilterPreferencesViewModel> _logger;

    private CancellationTokenSource? _debounceCts;
    private List<LocationGemeindeDto> _municipalities = [];
    private bool _isSyncing;
    private bool _suppressSearch;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool IsSaving { get; set; }

    [ObservableProperty]
    public partial bool ShowErrorMessage { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    // Immobilientyp-Filter
    [ObservableProperty]
    public partial bool IsHausSelected { get; set; }

    [ObservableProperty]
    public partial bool IsGrundstueckSelected { get; set; }

    [ObservableProperty]
    public partial bool IsZwangsversteigerungSelected { get; set; }

    // Anbietertyp-Filter
    [ObservableProperty]
    public partial bool IsPrivateSelected { get; set; }

    [ObservableProperty]
    public partial bool IsBrokerSelected { get; set; }

    /// <summary>
    /// Optionen fuer den Alters-Filter Picker (Index == AgeFilter Enum-Wert)
    /// </summary>
    public IReadOnlyList<string> AgeFilterOptions { get; } = ["Alle", "1 Tag", "1 Woche", "1 Monat", "1 Jahr"];

    [ObservableProperty]
    public partial int SelectedAgeFilterIndex { get; set; }

    /// <summary>
    /// Optionen fuer die Sortierung (Index == SortOption Enum-Wert)
    /// </summary>
    public IReadOnlyList<string> SortOptionLabels { get; } =
        ["Neueste", "Älteste", "Preis aufsteigend", "Preis absteigend", "Fläche absteigend", "Fläche aufsteigend", "PLZ aufsteigend"];

    [ObservableProperty]
    public partial int SelectedSortIndex { get; set; }

    // Ort-Auswahl
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

    public FilterPreferencesViewModel(
        IFilterPreferencesService filterPreferencesService,
        IFilterStateService filterStateService,
        ILocationService locationService,
        ILogger<FilterPreferencesViewModel> logger)
    {
        _filterPreferencesService = filterPreferencesService;
        _filterStateService = filterStateService;
        _locationService = locationService;
        _logger = logger;

        _isSyncing = true;
        IsHausSelected = true;
        IsGrundstueckSelected = true;
        IsZwangsversteigerungSelected = true;
        IsPrivateSelected = true;
        IsBrokerSelected = true;
        SelectedAgeFilterIndex = 0;
        SelectedSortIndex = 0;
        OrtSearchText = string.Empty;
        OrtSuggestions = [];
        _isSyncing = false;
    }

    #region IPageLifecycleAware

    public void OnAppearing()
    {
        _ = LoadPreferencesAsync();
    }

    public void OnDisappearing()
    {
        _debounceCts?.Cancel();
        // Beim Verlassen immer speichern, damit keine Aenderungen verloren gehen
        _ = SaveImmediatelyAsync();
    }

    #endregion

    private async Task LoadPreferencesAsync()
    {
        IsBusy = true;
        ShowErrorMessage = false;

        try
        {
            _municipalities = await _locationService.GetAllMunicipalitiesAsync();

            var preferences = await _filterPreferencesService.GetPreferencesAsync();
            ApplyPreferences(preferences ?? FilterPreferencesDto.Default);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[FilterPreferences] Load failed");
            ErrorMessage = "Einstellungen konnten nicht geladen werden.";
            ShowErrorMessage = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyPreferences(FilterPreferencesDto preferences)
    {
        _isSyncing = true;
        try
        {
            IsHausSelected = preferences.IsHausSelected;
            IsGrundstueckSelected = preferences.IsGrundstueckSelected;
            IsZwangsversteigerungSelected = preferences.IsZwangsversteigerungSelected;
            IsPrivateSelected = preferences.IsPrivateSelected;
            IsBrokerSelected = preferences.IsBrokerSelected;
            SelectedAgeFilterIndex = (int)preferences.SelectedAgeFilter;
            SelectedSortIndex = (int)preferences.SelectedSort;

            SelectedOrte.Clear();
            foreach (var ort in preferences.SelectedOrte)
                SelectedOrte.Add(ort);
            SelectedOrteCount = SelectedOrte.Count;
        }
        finally
        {
            _isSyncing = false;
        }
    }

    #region Auto-Save

    /// <summary>
    /// Startet einen Debounce-Timer (500ms). Wird bei jeder Filteraenderung aufgerufen.
    /// Mehrfache Aufrufe innerhalb der Verzoegerung setzen den Timer zurueck.
    /// </summary>
    private void ScheduleAutoSave()
    {
        if (_isSyncing) return;

        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;

        _ = AutoSaveAfterDelayAsync(token);
    }

    private async Task AutoSaveAfterDelayAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(500, token);
            if (token.IsCancellationRequested) return;

            IsSaving = true;
            ShowErrorMessage = false;

            var preferences = BuildPreferences();

            await _filterPreferencesService.SavePreferencesAsync(preferences);
            UpdateFilterStateService(preferences);
            _logger.LogInformation("[FilterPreferences] Auto-saved successfully");
        }
        catch (OperationCanceledException)
        {
            // Debounce abgebrochen - ignorieren
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[FilterPreferences] Auto-save failed");
            ErrorMessage = "Einstellungen konnten nicht gespeichert werden.";
            ShowErrorMessage = true;
            _ = HideErrorMessageAsync();
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>
    /// Speichert sofort ohne Debounce (beim Verlassen der Seite)
    /// </summary>
    private async Task SaveImmediatelyAsync()
    {
        try
        {
            var preferences = BuildPreferences();

            await _filterPreferencesService.SavePreferencesAsync(preferences);
            UpdateFilterStateService(preferences);
            _logger.LogInformation("[FilterPreferences] Saved on navigate-away");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[FilterPreferences] Save on navigate-away failed");
        }
    }

    private FilterPreferencesDto BuildPreferences() => new(
        SelectedOrte: SelectedOrte.ToList(),
        SelectedAgeFilter: (AgeFilter)Math.Clamp(SelectedAgeFilterIndex, 0, (int)AgeFilter.EinJahr),
        IsHausSelected: IsHausSelected,
        IsGrundstueckSelected: IsGrundstueckSelected,
        IsZwangsversteigerungSelected: IsZwangsversteigerungSelected,
        IsPrivateSelected: IsPrivateSelected,
        IsBrokerSelected: IsBrokerSelected,
        ExcludedSellerSourceIds: [],
        SelectedSort: (SortOption)Math.Clamp(SelectedSortIndex, 0, (int)SortOption.PlzAuf)
    );

    /// <summary>
    /// Synchronisiert gespeicherte Einstellungen in den FilterStateService,
    /// damit die HomePage die Aenderungen beim Zurueck-Navigieren uebernimmt.
    /// </summary>
    private void UpdateFilterStateService(FilterPreferencesDto preferences)
    {
        _filterStateService.UpdateFilters(
            preferences.IsHausSelected,
            preferences.IsGrundstueckSelected,
            preferences.IsZwangsversteigerungSelected,
            preferences.SelectedAgeFilter,
            preferences.SelectedOrte.ToList(),
            preferences.IsPrivateSelected,
            preferences.IsBrokerSelected,
            selectedSort: preferences.SelectedSort);
    }

    private async Task HideErrorMessageAsync()
    {
        await Task.Delay(3000);
        ShowErrorMessage = false;
    }

    #endregion

    #region Filteraenderungen (Typ-Filter: mindestens einer muss aktiv bleiben)

    partial void OnIsHausSelectedChanged(bool value)
    {
        if (_isSyncing) return;

        if (!value && !IsGrundstueckSelected && !IsZwangsversteigerungSelected)
        {
            _isSyncing = true;
            IsHausSelected = true;
            _isSyncing = false;
            return;
        }

        ScheduleAutoSave();
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

        ScheduleAutoSave();
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

        ScheduleAutoSave();
    }

    partial void OnIsPrivateSelectedChanged(bool value)
    {
        if (_isSyncing) return;

        if (!value && !IsBrokerSelected)
        {
            _isSyncing = true;
            IsPrivateSelected = true;
            _isSyncing = false;
            return;
        }

        ScheduleAutoSave();
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

        ScheduleAutoSave();
    }

    partial void OnSelectedAgeFilterIndexChanged(int value)
    {
        _ = value;
        ScheduleAutoSave();
    }

    partial void OnSelectedSortIndexChanged(int value)
    {
        _ = value;
        ScheduleAutoSave();
    }

    #endregion

    #region Commands

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

        ScheduleAutoSave();
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
            ScheduleAutoSave();
        }
    }

    #endregion
}
