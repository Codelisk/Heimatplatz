using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Features.Properties.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Models;
using Heimatplatz.Features.Properties.Models;
using UnoFramework.Contracts.Navigation;
using UnoFramework.Contracts.Pages;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// ViewModel fuer die FilterPreferencesPage.
/// Aenderungen an Filtern werden automatisch nach kurzer Verzoegerung gespeichert (Debounce).
/// </summary>
public partial class FilterPreferencesViewModel : ObservableObject, IPageInfo, INavigationAware
{
    private readonly IFilterPreferencesService _filterPreferencesService;
    private readonly ILocationService _locationService;

    private CancellationTokenSource? _debounceCts;
    private bool _isLoaded;

    #region IPageInfo Implementation

    public PageType PageType => PageType.Settings;
    public string PageTitle => "Filtereinstellungen";
    public Type? MainHeaderViewModel => null;

    #endregion

    #region INavigationAware Implementation

    /// <inheritdoc />
    public void OnNavigatedTo(object? parameter)
    {
        _ = LoadPreferencesAsync();
    }

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
        _debounceCts?.Cancel();
        UnsubscribeSellerTypes();
    }

    #endregion

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isSaving;

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
    private AgeFilter _selectedAgeFilter = AgeFilter.Alle;

    /// <summary>
    /// Liste der Anbietertypen fuer den SellerTypePicker
    /// </summary>
    [ObservableProperty]
    private List<SellerTypeModel> _sellerTypes = SellerTypeModel.CreateDefaultList();

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
        _isLoaded = false;
        IsBusy = true;
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
            ApplyPreferences(preferences ?? FilterPreferencesDto.Default);
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
            _isLoaded = true;
        }
    }

    #region Auto-Save

    /// <summary>
    /// Startet einen Debounce-Timer (500ms). Wird bei jeder Filteraenderung aufgerufen.
    /// Mehrfache Aufrufe innerhalb der Verzoegerung setzen den Timer zurueck.
    /// </summary>
    private void ScheduleAutoSave()
    {
        if (_isSyncing || !_isLoaded) return;

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

            var (isPrivate, isBroker, isPortal) = GetSellerTypeSelection();

            var preferences = new FilterPreferencesDto(
                SelectedOrte: SelectedOrte,
                SelectedAgeFilter: SelectedAgeFilter,
                IsHausSelected: IsHausSelected,
                IsGrundstueckSelected: IsGrundstueckSelected,
                IsZwangsversteigerungSelected: IsZwangsversteigerungSelected,
                IsPrivateSelected: isPrivate,
                IsBrokerSelected: isBroker,
                IsPortalSelected: isPortal,
                ExcludedSellerSourceIds: []
            );

            await _filterPreferencesService.SavePreferencesAsync(preferences);
            System.Diagnostics.Debug.WriteLine("[FilterPreferences] Auto-saved successfully");
        }
        catch (OperationCanceledException)
        {
            // Debounce abgebrochen - ignorieren
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FilterPreferences] Auto-save failed: {ex.Message}");
            ErrorMessage = "Einstellungen konnten nicht gespeichert werden.";
            ShowErrorMessage = true;
            _ = HideErrorMessageAsync();
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task HideErrorMessageAsync()
    {
        await Task.Delay(3000);
        ShowErrorMessage = false;
    }

    #endregion

    #region SellerType Subscriptions

    private void SubscribeSellerTypes()
    {
        foreach (var sellerType in SellerTypes)
        {
            sellerType.PropertyChanged += OnSellerTypePropertyChanged;
        }
    }

    private void UnsubscribeSellerTypes()
    {
        foreach (var sellerType in SellerTypes)
        {
            sellerType.PropertyChanged -= OnSellerTypePropertyChanged;
        }
    }

    private void OnSellerTypePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SellerTypeModel.IsSelected))
        {
            ScheduleAutoSave();
        }
    }

    #endregion

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

            // SellerTypes synchronisieren
            UnsubscribeSellerTypes();
            UpdateSellerTypesFromPreferences(
                preferences.IsPrivateSelected,
                preferences.IsBrokerSelected,
                preferences.IsPortalSelected);
            SubscribeSellerTypes();
        }
        finally
        {
            _isSyncing = false;
        }
    }

    private void UpdateSellerTypesFromPreferences(bool isPrivateSelected, bool isBrokerSelected, bool isPortalSelected)
    {
        foreach (var sellerType in SellerTypes)
        {
            sellerType.IsSelected = sellerType.Type switch
            {
                SellerType.Private => isPrivateSelected,
                SellerType.Broker => isBrokerSelected,
                SellerType.Portal => isPortalSelected,
                _ => true
            };
        }
    }

    private (bool IsPrivate, bool IsBroker, bool IsPortal) GetSellerTypeSelection()
    {
        return (
            SellerTypes.FirstOrDefault(st => st.Type == SellerType.Private)?.IsSelected ?? true,
            SellerTypes.FirstOrDefault(st => st.Type == SellerType.Broker)?.IsSelected ?? true,
            SellerTypes.FirstOrDefault(st => st.Type == SellerType.Portal)?.IsSelected ?? true
        );
    }

    // Typ-Filter Logik: Mindestens ein Filter muss aktiv bleiben
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

    partial void OnSelectedAgeFilterChanged(AgeFilter value)
    {
        ScheduleAutoSave();
    }

    partial void OnSelectedOrteChanged(List<string> value)
    {
        ScheduleAutoSave();
    }
}
