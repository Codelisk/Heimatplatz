using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Features.Properties.Models;

namespace Heimatplatz.Maui.Features.Properties.Presentation.Wizard;

/// <summary>
/// Eckdaten des Editors: Typ (Badge auf dem Hero-Foto), Titel, Zimmer, Flaechen,
/// Baujahr und Ausstattungs-Merkmale (Chips). Die Validierung spiegelt die
/// Server-Regeln (CreateProperty), damit Fehler VOR dem Veroeffentlichen mit
/// klarer Meldung auffallen statt als generisches 400.
/// </summary>
public partial class PropertyWizardViewModel
{
    // Befuellt im Konstruktor (PropertyWizardViewModel.cs) - die lokalisierten
    // Anzeigenamen brauchen das injizierte Loc.
    public List<PropertyTypeItem> PropertyTypes { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsHouseType))]
    [NotifyPropertyChangedFor(nameof(TypeBadgeText))]
    [NotifyPropertyChangedFor(nameof(TypeBadgeColor))]
    public partial PropertyTypeItem? SelectedPropertyTypeItem { get; set; }

    public bool IsHouseType => SelectedPropertyTypeItem?.Value == PropertyType.House;

    #region Typ-Badge (auf dem Hero-Foto, wie in der Detailansicht)

    /// <summary>Badge-Text wie auf der Detailseite/Karte (HAUS/GRUND)</summary>
    public string TypeBadgeText => SelectedPropertyTypeItem?.Value == PropertyType.Land
        ? Loc.TypeBadgeLand
        : Loc.TypeBadgeHouse;

    /// <summary>Badge-Farbe 1:1 wie PropertyDetailViewModel (Haus Blau, Grund Gruen)</summary>
    public Color TypeBadgeColor => SelectedPropertyTypeItem?.Value == PropertyType.Land
        ? Color.FromArgb("#33854A")
        : Color.FromArgb("#2F6E9E");

    /// <summary>Tipp auf das Badge: Typ per ActionSheet wechseln.</summary>
    [RelayCommand]
    private async Task EditTypeAsync()
    {
        var names = PropertyTypes.Select(t => t.DisplayName).ToArray();
        var choice = await Shell.Current.DisplayActionSheetAsync(Loc.PropertyTypePromptTitle, _common.Cancel, null, names);
        var picked = PropertyTypes.FirstOrDefault(t => t.DisplayName == choice);
        if (picked != null)
            SelectedPropertyTypeItem = picked;
    }

    #endregion

    [ObservableProperty]
    public partial string Titel { get; set; }

    [ObservableProperty]
    public partial string Zimmer { get; set; }

    [ObservableProperty]
    public partial string Wohnflaeche { get; set; }

    [ObservableProperty]
    public partial string Grundstuecksflaeche { get; set; }

    [ObservableProperty]
    public partial string Baujahr { get; set; }

    /// <summary>Link zum Originalinserat (optional, landet am Erst-Kontakt der Immobilie)</summary>
    [ObservableProperty]
    public partial string OriginalListingUrl { get; set; }

    #region Ausstattung & Merkmale (Chips)

    public ObservableCollection<string> FeatureItems { get; } = [];

    public bool HasFeatures => FeatureItems.Count > 0;

    [ObservableProperty]
    public partial string NewFeatureText { get; set; }

    /// <summary>Fuegt das getippte Merkmal hinzu (Komma trennt mehrere auf einmal).</summary>
    [RelayCommand]
    private void AddFeature()
    {
        var parts = (NewFeatureText ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            if (part.Length == 0 || part.Length > 100 || FeatureItems.Count >= 50)
                continue;
            if (FeatureItems.Any(f => string.Equals(f, part, StringComparison.OrdinalIgnoreCase)))
                continue;

            FeatureItems.Add(part);
        }

        NewFeatureText = string.Empty;
        OnPropertyChanged(nameof(HasFeatures));
        MarkDraftDirty();
        ScheduleAutoSave();
    }

    [RelayCommand]
    private void RemoveFeature(string feature)
    {
        FeatureItems.Remove(feature);
        OnPropertyChanged(nameof(HasFeatures));
        MarkDraftDirty();
        ScheduleAutoSave();
    }

    private void SetFeatures(IEnumerable<string> features)
    {
        FeatureItems.Clear();
        foreach (var feature in features)
            FeatureItems.Add(feature);
        OnPropertyChanged(nameof(HasFeatures));
    }

    #endregion

    private void InitializeDetailsStep()
    {
        Titel = string.Empty;
        Zimmer = string.Empty;
        Wohnflaeche = string.Empty;
        Grundstuecksflaeche = string.Empty;
        Baujahr = string.Empty;
        OriginalListingUrl = string.Empty;
        NewFeatureText = string.Empty;

        SelectedPropertyTypeItem = PropertyTypes[0]; // "Haus"
    }

    /// <summary>Spiegelt die CreateProperty-Serverregeln (Ranges), Meldungen wie im Web.</summary>
    private bool ValidateDetails()
    {
        // Leeres Feld und zu kurzer Titel getrennt melden: "mindestens 10 Zeichen"
        // ist verwirrend, wenn ueberhaupt nichts eingegeben wurde.
        if (string.IsNullOrWhiteSpace(Titel))
        {
            ErrorMessage = Loc.ValidationTitleRequired;
            return false;
        }

        if (Titel.Trim().Length < 10)
        {
            ErrorMessage = Loc.ValidationTitleTooShort;
            return false;
        }

        if (Titel.Trim().Length > 200)
        {
            ErrorMessage = Loc.ValidationTitleTooLong;
            return false;
        }

        if (!TryParseOptionalInt(Zimmer, Loc.FieldRooms, out var zimmer)
            || !TryParseOptionalInt(Wohnflaeche, Loc.FieldLivingArea, out var wohnflaeche)
            || !TryParseOptionalInt(Grundstuecksflaeche, Loc.FieldPlotArea, out var grund)
            || !TryParseOptionalInt(Baujahr, Loc.FieldYearBuilt, out var baujahr))
            return false;

        if (zimmer > 200)
        {
            ErrorMessage = Loc.ValidationRoomsRange;
            return false;
        }

        if (wohnflaeche > 10_000)
        {
            ErrorMessage = Loc.ValidationLivingAreaRange;
            return false;
        }

        if (grund > 1_000_000)
        {
            ErrorMessage = Loc.ValidationPlotAreaRange;
            return false;
        }

        if (baujahr is { } jahr && (jahr < 1000 || jahr > DateTime.Now.Year))
        {
            ErrorMessage = Loc.ValidationYearBuiltFuture;
            return false;
        }

        // Originalinserat: optional, aber wenn angegeben eine absolute http(s)-URL
        // (Spiegel von PropertyFieldValidation.NormalizeOriginalListingUrl)
        var originalUrl = OriginalListingUrl?.Trim();
        if (!string.IsNullOrEmpty(originalUrl)
            && (originalUrl.Length > 2000
                || !Uri.TryCreate(originalUrl, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
        {
            ErrorMessage = Loc.ValidationOriginalUrlInvalid;
            return false;
        }

        return true;
    }

    private bool TryParseOptionalInt(string value, string fieldName, out int? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(value))
            return true;

        // 0 ist kein sinnvoller Wert (der Server lehnt <= 0 ab) - Feld leer lassen heisst "keine Angabe"
        if (!int.TryParse(value, out var parsed) || parsed <= 0)
        {
            ErrorMessage = Loc.ValidationInvalidNumberFormat(fieldName);
            return false;
        }

        result = parsed;
        return true;
    }
}
