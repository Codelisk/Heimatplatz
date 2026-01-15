using Heimatplatz.Features.Properties.Contracts.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Heimatplatz.Features.Properties.Controls;

/// <summary>
/// PropertyCard - Zeigt eine Immobilie in der Listenansicht
/// </summary>
public sealed partial class PropertyCard : UserControl
{
    public PropertyCard()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// Die anzuzeigende Immobilie
    /// </summary>
    public static readonly DependencyProperty PropertyProperty =
        DependencyProperty.Register(
            nameof(Property),
            typeof(PropertyListItemDto),
            typeof(PropertyCard),
            new PropertyMetadata(null, OnPropertyChanged));

    public PropertyListItemDto? Property
    {
        get => (PropertyListItemDto?)GetValue(PropertyProperty);
        set => SetValue(PropertyProperty, value);
    }

    private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PropertyCard card && e.NewValue is PropertyListItemDto property)
        {
            card.UpdateDisplay(property);
        }
    }

    private void UpdateDisplay(PropertyListItemDto property)
    {
        // Preis formatieren
        PriceText.Text = $"{property.Preis:N0} €";

        // Titel und Adresse
        TitleText.Text = property.Titel;
        AddressText.Text = $"{property.Adresse}, {property.Ort}";

        // Flaeche
        var flaeche = property.WohnflaecheM2 ?? property.GrundstuecksflaecheM2;
        var flaechenTyp = property.WohnflaecheM2.HasValue ? "Wohnfläche" : "Grundstück";
        AreaText.Text = flaeche.HasValue ? $"{flaeche} m² {flaechenTyp}" : "Keine Angabe";

        // Zimmer (nur bei Haeusern anzeigen)
        if (property.Zimmer.HasValue && property.Typ == PropertyType.Haus)
        {
            RoomsPanel.Visibility = Visibility.Visible;
            RoomsText.Text = $"{property.Zimmer} Zimmer";
        }
        else
        {
            RoomsPanel.Visibility = Visibility.Collapsed;
        }

        // Anbieter Badge
        SellerBadgeText.Text = property.AnbieterTyp == SellerType.Privat ? "Privat" : "Makler";

        // Bild laden
        if (!string.IsNullOrEmpty(property.BildUrl))
        {
            PropertyImage.Source = new BitmapImage(new Uri(property.BildUrl));
        }
    }
}
