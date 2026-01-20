using Heimatplatz.Features.Properties.Contracts.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
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
        this.PointerEntered += OnCardPointerEntered;
        this.PointerExited += OnCardPointerExited;
    }

    /// <summary>
    /// Event wenn die Karte angeklickt wird
    /// </summary>
    public event EventHandler<PropertyListItemDto>? CardClicked;

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
        // Format price (compact)
        PriceText.Text = FormatPrice(property.Price);

        // City and address
        OrtText.Text = property.City;
        AddressText.Text = property.Address;

        // Title
        TitleText.Text = property.Title;

        // Type badge text and color
        TypeBadgeText.Text = property.Type switch
        {
            PropertyType.House => "HAUS",
            PropertyType.Land => "GRUND",
            PropertyType.Foreclosure => "ZV",
            _ => "IMM"
        };

        // Type-specific colors: HAUS=black, GRUND=green, ZV=red
        (TypeBadge.Background, TypeBadgeText.Foreground) = property.Type switch
        {
            PropertyType.House => (
                new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 45, 55, 72)),
                new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White)),
            PropertyType.Land => (
                new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 34, 139, 34)),
                new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White)),
            PropertyType.Foreclosure => (
                new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Firebrick),
                new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White)),
            _ => (
                (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AccentBrush"],
                (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["ChipSelectedForegroundBrush"])
        };

        // Only show HAUS and GRUND, hide other types
        TypeBadge.Visibility = property.Type switch
        {
            PropertyType.House => Visibility.Visible,
            PropertyType.Land => Visibility.Visible,
            _ => Visibility.Collapsed
        };

        // Plot area
        GrundstueckText.Text = property.PlotAreaM2?.ToString("N0") ?? "—";

        // Living area
        if (property.LivingAreaM2.HasValue)
        {
            WohnflaechePanel.Visibility = Visibility.Visible;
            WohnflaecheText.Text = property.LivingAreaM2.Value.ToString("N0");
        }
        else
        {
            WohnflaechePanel.Visibility = Visibility.Collapsed;
        }

        // Rooms
        if (property.Rooms.HasValue)
        {
            RoomsPanel.Visibility = Visibility.Visible;
            RoomsText.Text = property.Rooms.Value.ToString();
        }
        else
        {
            RoomsPanel.Visibility = Visibility.Collapsed;
        }

        // Seller (compact)
        SellerBadgeText.Text = property.SellerType == SellerType.Privat ? "Privat" : property.SellerName;

        // Load images (FlipView for swipe)
        if (property.ImageUrls?.Count > 0)
        {
            var imageUrls = property.ImageUrls.Where(url => !string.IsNullOrEmpty(url)).ToList();
            ImageFlipView.ItemsSource = imageUrls;

            // Show counter and arrows when multiple images
            if (imageUrls.Count > 1)
            {
                ImageCounterBadge.Visibility = Visibility.Visible;
                ImageCounterText.Text = $"1/{imageUrls.Count}";

                // Show navigation arrows
                PrevImageButton.Visibility = Visibility.Visible;
                NextImageButton.Visibility = Visibility.Visible;

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

    private void OnCardPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (Property != null)
        {
            CardClicked?.Invoke(this, Property);
        }
    }

    private void OnPrevImageClick(object sender, RoutedEventArgs e)
    {
        if (ImageFlipView.SelectedIndex > 0)
        {
            ImageFlipView.SelectedIndex--;
        }
        else if (ImageFlipView.Items.Count > 0)
        {
            // Wrap zum letzten Bild
            ImageFlipView.SelectedIndex = ImageFlipView.Items.Count - 1;
        }
    }

    private void OnPrevImagePointerPressed(object sender, PointerRoutedEventArgs e)
    {
        // Verhindere Navigation zur Detail-Seite
        e.Handled = true;
    }

    private void OnNextImageClick(object sender, RoutedEventArgs e)
    {
        if (ImageFlipView.SelectedIndex < ImageFlipView.Items.Count - 1)
        {
            ImageFlipView.SelectedIndex++;
        }
        else
        {
            // Wrap zum ersten Bild
            ImageFlipView.SelectedIndex = 0;
        }
    }

    private void OnNextImagePointerPressed(object sender, PointerRoutedEventArgs e)
    {
        // Verhindere Navigation zur Detail-Seite
        e.Handled = true;
    }

    private void OnCardPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        // Hover-Effekt: Leichte Skalierung
        this.Scale = new System.Numerics.Vector3(1.02f, 1.02f, 1f);
    }

    private void OnCardPointerExited(object sender, PointerRoutedEventArgs e)
    {
        // Hover-Effekt zuruecksetzen
        this.Scale = new System.Numerics.Vector3(1f, 1f, 1f);
    }
}
