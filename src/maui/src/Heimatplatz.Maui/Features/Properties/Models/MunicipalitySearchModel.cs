using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Maui.Features.Properties.Services;
using Microsoft.Extensions.Logging;

namespace Heimatplatz.Maui.Features.Properties.Models;

/// <summary>
/// Wiederverwendbare Gemeinde-Suche (Suchfeld + Vorschlagsliste + Auswahl) -
/// extrahiert aus der bislang in Add/AiAdd/Edit dreifach duplizierten Logik.
/// Wird als Unterobjekt in ein Seiten-ViewModel eingehaengt (Bindings via z.B. "Ort.OrtSearchText").
/// </summary>
public partial class MunicipalitySearchModel : ObservableObject
{
    private readonly ILocationService _locationService;
    private readonly ILogger _logger;

    private List<LocationGemeindeDto> _municipalities = [];
    private bool _suppressSearch;

    [ObservableProperty]
    public partial string OrtSearchText { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasOrtSuggestions))]
    public partial List<LocationGemeindeDto> OrtSuggestions { get; set; }

    public bool HasOrtSuggestions => OrtSuggestions.Count > 0;

    [ObservableProperty]
    public partial Guid? SelectedGemeindeId { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedOrt))]
    [NotifyPropertyChangedFor(nameof(HasNoSelectedOrt))]
    public partial string SelectedOrtText { get; set; }

    public bool HasSelectedOrt => !string.IsNullOrEmpty(SelectedOrtText);

    /// <summary>Suchfeld nur zeigen solange kein Ort gewaehlt ist - es ist genau EIN Ort waehlbar.</summary>
    public bool HasNoSelectedOrt => !HasSelectedOrt;

    public MunicipalitySearchModel(ILocationService locationService, ILogger logger)
    {
        _locationService = locationService;
        _logger = logger;

        OrtSearchText = string.Empty;
        OrtSuggestions = [];
        SelectedOrtText = string.Empty;
    }

    /// <summary>Laedt die Gemeindeliste einmalig (der LocationService cached selbst).</summary>
    public async Task EnsureLoadedAsync()
    {
        if (_municipalities.Count > 0)
            return;

        try
        {
            _municipalities = await _locationService.GetAllMunicipalitiesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MunicipalitySearch] Fehler beim Laden der Orte");
        }
    }

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
            .Where(m => m.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                     || m.PostalCode.StartsWith(search, StringComparison.OrdinalIgnoreCase))
            .Take(15)
            .ToList();
    }

    [RelayCommand]
    private void SelectGemeinde(LocationGemeindeDto gemeinde)
    {
        SelectedGemeindeId = gemeinde.Id;
        SelectedOrtText = $"{gemeinde.Name} ({gemeinde.PostalCode})";

        _suppressSearch = true;
        OrtSearchText = string.Empty;
        _suppressSearch = false;
        OrtSuggestions = [];
    }

    /// <summary>Hebt die Auswahl auf, damit ein anderer Ort gesucht werden kann.</summary>
    [RelayCommand]
    private void ClearSelection()
    {
        SelectedGemeindeId = null;
        SelectedOrtText = string.Empty;

        _suppressSearch = true;
        OrtSearchText = string.Empty;
        _suppressSearch = false;
        OrtSuggestions = [];
    }

    /// <summary>
    /// Stellt die Auswahl aus einem gespeicherten Entwurf wieder her - der Anzeige-Text
    /// kommt aus dem Entwurf, damit kein Gemeinde-Lookup noetig ist.
    /// </summary>
    public void Restore(Guid municipalityId, string displayText)
    {
        SelectedGemeindeId = municipalityId;
        SelectedOrtText = displayText;
    }
}
