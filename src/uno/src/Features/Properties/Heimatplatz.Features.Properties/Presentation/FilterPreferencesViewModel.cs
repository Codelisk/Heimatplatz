using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Features.Properties.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Models;
using Heimatplatz.Features.Properties.Models;
using UnoFramework.Contracts.Navigation;
using UnoFramework.Contracts.Pages;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// ViewModel fuer die FilterPreferencesPage
/// Implements INavigationAware to trigger PageNavigatedEvent for header updates
/// </summary>
public partial class FilterPreferencesViewModel : ObservableObject, IPageInfo, INavigationAware
{
    private readonly IFilterPreferencesService _filterPreferencesService;
    private readonly ILocationService _locationService;

    #region IPageInfo Implementation

    public PageType PageType => PageType.Settings;
    public string PageTitle => "Filtereinstellungen";
    public Type? MainHeaderViewModel => null;

    #endregion

    #region INavigationAware Implementation

    /// <inheritdoc />
    public void OnNavigatedTo(object? parameter)
    {
        // Load preferences when navigated to
        _ = LoadPreferencesAsync();
    }

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
        // Cleanup if needed
    }

    #endregion

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private bool _showSuccessMessage;

    [ObservableProperty]
    private bool _showErrorMessage;

    [ObservableProperty]
    private string? _errorMessage;

    // Filter Properties
    [ObservableProperty]
    private bool _isHausSelected = true;

    [ObservableProperty]
    private bool _isGrundstueckSelected = true;

    [ObservableProperty]
    private bool _isZwangsversteigerungSelected = true;

    [ObservableProperty]
    private bool _isPrivateSelected = true;

    [ObservableProperty]
    private bool _isBrokerSelected = true;

    [ObservableProperty]
    private bool _isPortalSelected = true;

    [ObservableProperty]
    private AgeFilter _selectedAgeFilter = AgeFilter.Alle;

    [ObservableProperty]
    private List<string> _selectedOrte = [];

    /// <summary>
    /// Liste der Bezirke (von API geladen)
    /// </summary>
    [ObservableProperty]
    private List<BezirkModel> _bezirke = [];

    public FilterPreferencesViewModel(IFilterPreferencesService filterPreferencesService, ILocationService locationService)
    {
        _filterPreferencesService = filterPreferencesService;
        _locationService = locationService;
    }

    /// <summary>
    /// Wird von der Page beim Laden aufgerufen
    /// </summary>
    [RelayCommand]
    private async Task LoadPreferencesAsync()
    {
        IsBusy = true;
        ShowSuccessMessage = false;
        ShowErrorMessage = false;

        try
        {
            // Bezirke mit Gemeinden von API laden
            var locations = await _locationService.GetLocationsAsync();
            Bezirke = locations
                .SelectMany(bl => bl.Bezirke)
                .Select(b => new BezirkModel(
                    b.Id,
                    b.Name,
                    b.Gemeinden.Select(g => new GemeindeModel(g.Id, g.Name, g.PostalCode)).ToList()
                ))
                .ToList();

            var preferences = await _filterPreferencesService.GetPreferencesAsync();

            if (preferences != null)
            {
                ApplyPreferences(preferences);
            }
            else
            {
                // Standardwerte setzen
                ApplyPreferences(FilterPreferencesDto.Default);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FilterPreferences] Load failed: {ex.Message}");
            ErrorMessage = "Einstellungen konnten nicht geladen werden.";
            ShowErrorMessage = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Speichert die aktuellen Filtereinstellungen
    /// </summary>
    [RelayCommand]
    private async Task SavePreferencesAsync()
    {
        IsSaving = true;
        ShowSuccessMessage = false;
        ShowErrorMessage = false;

        try
        {
            var preferences = new FilterPreferencesDto(
                SelectedOrte: SelectedOrte,
                SelectedAgeFilter: SelectedAgeFilter,
                IsHausSelected: IsHausSelected,
                IsGrundstueckSelected: IsGrundstueckSelected,
                IsZwangsversteigerungSelected: IsZwangsversteigerungSelected,
                IsPrivateSelected: IsPrivateSelected,
                IsBrokerSelected: IsBrokerSelected,
                IsPortalSelected: IsPortalSelected,
                ExcludedSellerSourceIds: []
            );

            await _filterPreferencesService.SavePreferencesAsync(preferences);

            ShowSuccessMessage = true;

            // Erfolgsmeldung nach 3 Sekunden ausblenden
            _ = HideSuccessMessageAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FilterPreferences] Save failed: {ex.Message}");
            ErrorMessage = "Einstellungen konnten nicht gespeichert werden.";
            ShowErrorMessage = true;
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task HideSuccessMessageAsync()
    {
        await Task.Delay(3000);
        ShowSuccessMessage = false;
    }

    private bool _isSyncing;

    private void ApplyPreferences(FilterPreferencesDto preferences)
    {
        _isSyncing = true;
        try
        {
            SelectedOrte = preferences.SelectedOrte.ToList();
            SelectedAgeFilter = preferences.SelectedAgeFilter;
            IsHausSelected = preferences.IsHausSelected;
            IsGrundstueckSelected = preferences.IsGrundstueckSelected;
            IsZwangsversteigerungSelected = preferences.IsZwangsversteigerungSelected;
            IsPrivateSelected = preferences.IsPrivateSelected;
            IsBrokerSelected = preferences.IsBrokerSelected;
            IsPortalSelected = preferences.IsPortalSelected;
        }
        finally
        {
            _isSyncing = false;
        }
    }

    // Typ-Filter Logik: Mindestens ein Filter muss aktiv bleiben
    partial void OnIsHausSelectedChanged(bool value)
    {
        if (_isSyncing) return;

        // Mindestens ein Filter muss aktiv bleiben
        if (!value && !IsGrundstueckSelected && !IsZwangsversteigerungSelected)
        {
            _isSyncing = true;
            IsHausSelected = true;
            _isSyncing = false;
        }
    }

    partial void OnIsGrundstueckSelectedChanged(bool value)
    {
        if (_isSyncing) return;

        // Mindestens ein Filter muss aktiv bleiben
        if (!value && !IsHausSelected && !IsZwangsversteigerungSelected)
        {
            _isSyncing = true;
            IsGrundstueckSelected = true;
            _isSyncing = false;
        }
    }

    partial void OnIsZwangsversteigerungSelectedChanged(bool value)
    {
        if (_isSyncing) return;

        // Mindestens ein Filter muss aktiv bleiben
        if (!value && !IsHausSelected && !IsGrundstueckSelected)
        {
            _isSyncing = true;
            IsZwangsversteigerungSelected = true;
            _isSyncing = false;
        }
    }

    partial void OnIsPrivateSelectedChanged(bool value)
    {
        if (_isSyncing) return;

        if (!value && !IsBrokerSelected && !IsPortalSelected)
        {
            _isSyncing = true;
            IsPrivateSelected = true;
            _isSyncing = false;
        }
    }

    partial void OnIsBrokerSelectedChanged(bool value)
    {
        if (_isSyncing) return;

        if (!value && !IsPrivateSelected && !IsPortalSelected)
        {
            _isSyncing = true;
            IsBrokerSelected = true;
            _isSyncing = false;
        }
    }

    partial void OnIsPortalSelectedChanged(bool value)
    {
        if (_isSyncing) return;

        if (!value && !IsPrivateSelected && !IsBrokerSelected)
        {
            _isSyncing = true;
            IsPortalSelected = true;
            _isSyncing = false;
        }
    }
}
