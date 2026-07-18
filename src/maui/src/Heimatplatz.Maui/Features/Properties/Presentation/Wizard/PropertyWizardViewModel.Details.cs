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
    public List<PropertyTypeItem> PropertyTypes { get; } = PropertyTypeItem.GetAll();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsHouseType))]
    [NotifyPropertyChangedFor(nameof(TypeBadgeText))]
    [NotifyPropertyChangedFor(nameof(TypeBadgeColor))]
    public partial PropertyTypeItem? SelectedPropertyTypeItem { get; set; }

    public bool IsHouseType => SelectedPropertyTypeItem?.Value == PropertyType.House;

    #region Typ-Badge (auf dem Hero-Foto, wie in der Detailansicht)

    /// <summary>Badge-Text wie auf der Detailseite/Karte (HAUS/GRUND)</summary>
    public string TypeBadgeText => SelectedPropertyTypeItem?.Value == PropertyType.Land ? "GRUND" : "HAUS";

    /// <summary>Badge-Farbe 1:1 wie PropertyDetailViewModel (Haus Blau, Grund Gruen)</summary>
    public Color TypeBadgeColor => SelectedPropertyTypeItem?.Value == PropertyType.Land
        ? Color.FromArgb("#33854A")
        : Color.FromArgb("#2F6E9E");

    /// <summary>Tipp auf das Badge: Typ per ActionSheet wechseln.</summary>
    [RelayCommand]
    private async Task EditTypeAsync()
    {
        var names = PropertyTypes.Select(t => t.DisplayName).ToArray();
        var choice = await Shell.Current.DisplayActionSheetAsync("Immobilientyp", "Abbrechen", null, names);
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
        NewFeatureText = string.Empty;

        SelectedPropertyTypeItem = PropertyTypes[0]; // "Haus"
    }

    /// <summary>Spiegelt die CreateProperty-Serverregeln (Ranges), Meldungen wie im Web.</summary>
    private bool ValidateDetails()
    {
        if (string.IsNullOrWhiteSpace(Titel) || Titel.Trim().Length < 10)
        {
            ErrorMessage = "Titel muss mindestens 10 Zeichen lang sein";
            return false;
        }

        if (Titel.Trim().Length > 200)
        {
            ErrorMessage = "Titel darf höchstens 200 Zeichen lang sein";
            return false;
        }

        if (!TryParseOptionalInt(Zimmer, "Zimmer", out var zimmer)
            || !TryParseOptionalInt(Wohnflaeche, "Wohnfläche", out var wohnflaeche)
            || !TryParseOptionalInt(Grundstuecksflaeche, "Grundstücksfläche", out var grund)
            || !TryParseOptionalInt(Baujahr, "Baujahr", out var baujahr))
            return false;

        if (zimmer > 200)
        {
            ErrorMessage = "Bitte geben Sie eine realistische Zimmeranzahl an (1–200)";
            return false;
        }

        if (wohnflaeche > 10_000)
        {
            ErrorMessage = "Bitte geben Sie eine realistische Wohnfläche an (bis 10.000 m²)";
            return false;
        }

        if (grund > 1_000_000)
        {
            ErrorMessage = "Bitte geben Sie eine realistische Grundstücksfläche an (bis 1.000.000 m²)";
            return false;
        }

        if (baujahr is { } jahr && (jahr < 1000 || jahr > DateTime.Now.Year))
        {
            ErrorMessage = "Das Baujahr darf nicht in der Zukunft liegen";
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
            ErrorMessage = $"Bitte geben Sie einen gültigen Wert für \"{fieldName}\" ein (größer als 0)";
            return false;
        }

        result = parsed;
        return true;
    }
}
