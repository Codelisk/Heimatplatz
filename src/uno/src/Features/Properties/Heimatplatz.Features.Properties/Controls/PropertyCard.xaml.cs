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
        // Preis formatieren (kompakt)
        PriceText.Text = FormatPrice(property.Preis);

        // Ort und Adresse (neue Struktur)
        OrtText.Text = property.Ort;
        AddressText.Text = property.Adresse;

        // Titel
        TitleText.Text = property.Titel;

        // Typ-Badge
        TypeBadgeText.Text = property.Typ switch
        {
            PropertyType.Haus => "HAUS",
            PropertyType.Grundstueck => "GRUND",
            PropertyType.Zwangsversteigerung => "ZV",
            _ => "IMM"
        };

        // Zwangsversteigerung visuell hervorheben
        if (property.Typ == PropertyType.Zwangsversteigerung)
        {
            TypeBadge.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Microsoft.UI.Colors.Firebrick);
            TypeBadgeText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Microsoft.UI.Colors.White);
        }

        // Grundstuecksflaeche
        GrundstueckText.Text = property.GrundstuecksflaecheM2?.ToString("N0") ?? "—";

        // Wohnflaeche
        if (property.WohnflaecheM2.HasValue)
        {
            WohnflaechePanel.Visibility = Visibility.Visible;
            WohnflaecheText.Text = property.WohnflaecheM2.Value.ToString("N0");
        }
        else
        {
            WohnflaechePanel.Visibility = Visibility.Collapsed;
        }

        // Zimmer
        if (property.Zimmer.HasValue)
        {
            RoomsPanel.Visibility = Visibility.Visible;
            RoomsText.Text = property.Zimmer.Value.ToString();
        }
        else
        {
            RoomsPanel.Visibility = Visibility.Collapsed;
        }

        // Anbieter (kompakt)
        SellerBadgeText.Text = property.AnbieterTyp == SellerType.Privat ? "Privat" : property.AnbieterName;

        // Bilder laden (FlipView fuer Swipe)
        if (property.BildUrls?.Count > 0)
        {
            var imageUrls = property.BildUrls.Where(url => !string.IsNullOrEmpty(url)).ToList();
            ImageFlipView.ItemsSource = imageUrls;

            // Counter anzeigen wenn mehrere Bilder
            if (imageUrls.Count > 1)
            {
                ImageCounterBadge.Visibility = Visibility.Visible;
                ImageCounterText.Text = $"1/{imageUrls.Count}";
                ImageFlipView.SelectionChanged += (s, e) =>
                {
                    ImageCounterText.Text = $"{ImageFlipView.SelectedIndex + 1}/{imageUrls.Count}";
                };
            }
        }
    }

    private static string FormatPrice(decimal price)
    {
        if (price >= 1_000_000)
            return $"{price / 1_000_000:0.##} Mio €";
        if (price >= 1_000)
            return $"{price / 1_000:0} T€";
        return $"{price:N0} €";
    }
}
