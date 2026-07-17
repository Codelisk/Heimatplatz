using CommunityToolkit.Mvvm.ComponentModel;
using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Features.Properties.Models;

namespace Heimatplatz.Maui.Features.Properties.Presentation.Wizard;

/// <summary>
/// Schritt 2: Eckdaten - Typ, Titel, Zimmer, Flaechen, Baujahr und Ausstattung
/// werden bewusst manuell erfasst (keine KI). Die Beschreibung folgt erst in
/// Schritt 4 (.Description).
/// </summary>
public partial class PropertyWizardViewModel
{
    public List<PropertyTypeItem> PropertyTypes { get; } = PropertyTypeItem.GetAll();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsHouseType))]
    public partial PropertyTypeItem? SelectedPropertyTypeItem { get; set; }

    public bool IsHouseType => SelectedPropertyTypeItem?.Value == PropertyType.House;

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

    [ObservableProperty]
    public partial string FeaturesText { get; set; }

    private void InitializeDetailsStep()
    {
        Titel = string.Empty;
        Zimmer = string.Empty;
        Wohnflaeche = string.Empty;
        Grundstuecksflaeche = string.Empty;
        Baujahr = string.Empty;
        FeaturesText = string.Empty;

        SelectedPropertyTypeItem = PropertyTypes[0]; // "Haus"
    }

    private bool ValidateDetails()
    {
        if (string.IsNullOrWhiteSpace(Titel) || Titel.Trim().Length < 10)
        {
            ErrorMessage = "Titel muss mindestens 10 Zeichen lang sein";
            return false;
        }

        return TryParseOptionalInt(Zimmer, "Zimmer", out _)
            && TryParseOptionalInt(Wohnflaeche, "Wohnfläche", out _)
            && TryParseOptionalInt(Grundstuecksflaeche, "Grundstücksfläche", out _)
            && TryParseOptionalInt(Baujahr, "Baujahr", out _);
    }

    private bool TryParseOptionalInt(string value, string fieldName, out int? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(value))
            return true;

        if (!int.TryParse(value, out var parsed) || parsed < 0)
        {
            ErrorMessage = $"Bitte geben Sie einen gültigen Wert für \"{fieldName}\" ein";
            return false;
        }

        result = parsed;
        return true;
    }
}
