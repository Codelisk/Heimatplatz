using CommunityToolkit.Mvvm.ComponentModel;

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

    private void InitializeLocationPriceStep()
    {
        Preis = string.Empty;
        Adresse = string.Empty;
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
