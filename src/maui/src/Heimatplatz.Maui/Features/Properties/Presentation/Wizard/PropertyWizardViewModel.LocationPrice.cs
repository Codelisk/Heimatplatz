using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Maui.ApiClient.Generated;

namespace Heimatplatz.Maui.Features.Properties.Presentation.Wizard;

/// <summary>
/// Schritt 3: Lage &amp; Preis - die Felder, die die KI nie liefert.
/// Der Nutzer fuellt sie aus, waehrend die Analyse im Hintergrund laeuft.
/// Gemeinde-Suche via <see cref="PropertyWizardViewModel.Ort"/> (MunicipalitySearchModel).
/// </summary>
public partial class PropertyWizardViewModel
{
    [ObservableProperty]
    public partial string Preis { get; set; }

    [ObservableProperty]
    public partial string Adresse { get; set; }

    /// <summary>
    /// Lage-Anzeige (Genau/Ungefaehr/Verborgen) wie im Web-Editor: Der Anbieter
    /// entscheidet, wie genau die Karte sein Inserat zeigt. Standard = Ungefaehr
    /// (entspricht dem Server-Default fuer Clients ohne das Feld).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLocationApproximate))]
    [NotifyPropertyChangedFor(nameof(IsLocationExact))]
    [NotifyPropertyChangedFor(nameof(IsLocationHidden))]
    public partial LocationDisplayMode SelectedLocationDisplay { get; set; }

    // RadioButton-Bindings (TwoWay): false-Zuweisungen des Gruppen-Umschaltens ignorieren
    public bool IsLocationApproximate
    {
        get => SelectedLocationDisplay == LocationDisplayMode.Approximate;
        set { if (value) SelectedLocationDisplay = LocationDisplayMode.Approximate; }
    }

    public bool IsLocationExact
    {
        get => SelectedLocationDisplay == LocationDisplayMode.Exact;
        set { if (value) SelectedLocationDisplay = LocationDisplayMode.Exact; }
    }

    public bool IsLocationHidden
    {
        get => SelectedLocationDisplay == LocationDisplayMode.Hidden;
        set { if (value) SelectedLocationDisplay = LocationDisplayMode.Hidden; }
    }

    /// <summary>Zeilen-Tap der Lage-Anzeige-Auswahl (ganze Zeile antippbar, wie RegisterPage).</summary>
    [RelayCommand]
    private void SelectLocationDisplay(string mode)
    {
        if (Enum.TryParse<LocationDisplayMode>(mode, out var parsed))
            SelectedLocationDisplay = parsed;
    }

    private void InitializeLocationPriceStep()
    {
        Preis = string.Empty;
        Adresse = string.Empty;
        SelectedLocationDisplay = LocationDisplayMode.Approximate;
    }

    private bool ValidateLocationPrice()
    {
        if (!decimal.TryParse(Preis, out var preisValue) || preisValue <= 0)
        {
            ErrorMessage = Loc.ValidationPriceInvalid;
            return false;
        }

        if (string.IsNullOrWhiteSpace(Adresse))
        {
            ErrorMessage = Loc.ValidationStreetMissing;
            return false;
        }

        if (!Ort.SelectedGemeindeId.HasValue)
        {
            ErrorMessage = Loc.ValidationOrtMissing;
            return false;
        }

        return true;
    }
}
