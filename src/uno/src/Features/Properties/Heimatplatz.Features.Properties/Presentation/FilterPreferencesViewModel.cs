using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Features.Properties.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Models;
using Heimatplatz.Features.Properties.Models;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// ViewModel fuer die FilterPreferencesPage
/// </summary>
public partial class FilterPreferencesViewModel : ObservableObject
{
    private readonly IFilterPreferencesService _filterPreferencesService;

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
    private bool _isAllSelected = true;

    [ObservableProperty]
    private bool _isHausSelected;

    [ObservableProperty]
    private bool _isGrundstueckSelected;

    [ObservableProperty]
    private bool _isZwangsversteigerungSelected;

    [ObservableProperty]
    private AgeFilter _selectedAgeFilter = AgeFilter.Alle;

    [ObservableProperty]
    private List<string> _selectedOrte = [];

    /// <summary>
    /// Hierarchische Bezirk/Ort-Struktur (gleich wie HomePage)
    /// </summary>
    public List<BezirkModel> Bezirke { get; } =
    [
        new BezirkModel("Linz-Land", "Traun", "Leonding", "Ansfelden", "Pasching", "Hörsching"),
        new BezirkModel("Linz-Stadt", "Linz", "Urfahr"),
        new BezirkModel("Wels-Land", "Wels", "Marchtrenk", "Gunskirchen"),
        new BezirkModel("Steyr-Land", "Steyr", "Sierning", "Garsten"),
    ];

    public FilterPreferencesViewModel(IFilterPreferencesService filterPreferencesService)
    {
        _filterPreferencesService = filterPreferencesService;
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
                IsAllSelected: IsAllSelected,
                IsHausSelected: IsHausSelected,
                IsGrundstueckSelected: IsGrundstueckSelected,
                IsZwangsversteigerungSelected: IsZwangsversteigerungSelected
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

    private void ApplyPreferences(FilterPreferencesDto preferences)
    {
        SelectedOrte = preferences.SelectedOrte.ToList();
        SelectedAgeFilter = preferences.SelectedAgeFilter;
        IsAllSelected = preferences.IsAllSelected;
        IsHausSelected = preferences.IsHausSelected;
        IsGrundstueckSelected = preferences.IsGrundstueckSelected;
        IsZwangsversteigerungSelected = preferences.IsZwangsversteigerungSelected;
    }

    // Typ-Filter Logik (gleich wie HomePage)
    partial void OnIsAllSelectedChanged(bool value)
    {
        if (value)
        {
            IsHausSelected = false;
            IsGrundstueckSelected = false;
            IsZwangsversteigerungSelected = false;
        }
    }

    partial void OnIsHausSelectedChanged(bool value)
    {
        if (value)
        {
            IsAllSelected = false;
        }
    }

    partial void OnIsGrundstueckSelectedChanged(bool value)
    {
        if (value)
        {
            IsAllSelected = false;
        }
    }

    partial void OnIsZwangsversteigerungSelectedChanged(bool value)
    {
        if (value)
        {
            IsAllSelected = false;
        }
    }
}
