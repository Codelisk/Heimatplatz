using Heimatplatz.Features.Properties.Contracts.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Heimatplatz.Features.Properties.Controls;

/// <summary>
/// PropertyCard - Zeigt eine Immobilie in der Listenansicht
/// </summary>
public sealed partial class PropertyCard : UserControl
{
    private MenuFlyoutItem? _favoriteMenuItem;
    private MenuFlyoutItem? _blockMenuItem;

    public PropertyCard()
    {
        this.InitializeComponent();
        this.PointerEntered += OnCardPointerEntered;
        this.PointerExited += OnCardPointerExited;
        this.Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Get references to menu items for dynamic text updates
        if (MoreOptionsButton.Flyout is MenuFlyout menuFlyout)
        {
            foreach (var item in menuFlyout.Items)
            {
                if (item is MenuFlyoutItem menuItem)
                {
                    if (menuItem.Text.Contains("avorisieren"))
                        _favoriteMenuItem = menuItem;
                    else if (menuItem.Text.Contains("lockieren"))
                        _blockMenuItem = menuItem;
                }
            }
        }
        UpdateMenuTexts();
    }

    /// <summary>
    /// Event wenn die Karte angeklickt wird
    /// </summary>
    public event EventHandler<PropertyListItemDto>? CardClicked;

    /// <summary>
    /// Event wenn die Immobilie blockiert/entblockiert werden soll (Toggle)
    /// </summary>
    public event EventHandler<PropertyListItemDto>? PropertyBlocked;

    /// <summary>
    /// Event wenn die Immobilie favorisiert/entfavorisiert werden soll (Toggle)
    /// </summary>
    public event EventHandler<PropertyListItemDto>? PropertyFavorited;

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

    /// <summary>
    /// Ob diese Immobilie favorisiert ist
    /// </summary>
    public static readonly DependencyProperty IsFavoriteProperty =
        DependencyProperty.Register(
            nameof(IsFavorite),
            typeof(bool),
            typeof(PropertyCard),
            new PropertyMetadata(false, OnStatusChanged));

    public bool IsFavorite
    {
        get => (bool)GetValue(IsFavoriteProperty);
        set => SetValue(IsFavoriteProperty, value);
    }

    /// <summary>
    /// Ob diese Immobilie blockiert ist
    /// </summary>
    public static readonly DependencyProperty IsBlockedProperty =
        DependencyProperty.Register(
            nameof(IsBlocked),
            typeof(bool),
            typeof(PropertyCard),
            new PropertyMetadata(false, OnStatusChanged));

    public bool IsBlocked
    {
        get => (bool)GetValue(IsBlockedProperty);
        set => SetValue(IsBlockedProperty, value);
    }

    /// <summary>
    /// Ob der More-Button angezeigt werden soll (false fuer Favoriten/Blockierte Seiten)
    /// </summary>
    public static readonly DependencyProperty ShowMoreButtonProperty =
        DependencyProperty.Register(
            nameof(ShowMoreButton),
            typeof(bool),
            typeof(PropertyCard),
            new PropertyMetadata(true, OnShowMoreButtonChanged));

    public bool ShowMoreButton
    {
        get => (bool)GetValue(ShowMoreButtonProperty);
        set => SetValue(ShowMoreButtonProperty, value);
    }

    /// <summary>
    /// Der Anzeigemodus der Card (Default, Favorite, Blocked)
    /// </summary>
    public static readonly DependencyProperty ModeProperty =
        DependencyProperty.Register(
            nameof(Mode),
            typeof(CardMode),
            typeof(PropertyCard),
            new PropertyMetadata(CardMode.Default, OnModeChanged));

    public CardMode Mode
    {
        get => (CardMode)GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PropertyCard card && e.NewValue is PropertyListItemDto property)
        {
            card.UpdateDisplay(property);
        }
    }

    private static void OnStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PropertyCard card)
        {
            card.UpdateMenuTexts();
        }
    }

    private static void OnShowMoreButtonChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PropertyCard card)
        {
            card.MoreOptionsButton.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private static void OnModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PropertyCard card)
        {
            card.UpdateActionButtonVisibility();
        }
    }

    private void UpdateActionButtonVisibility()
    {
        MoreOptionsButton.Visibility = Mode == CardMode.Default ? Visibility.Visible : Visibility.Collapsed;
        FavoriteActionButton.Visibility = Mode == CardMode.Favorite ? Visibility.Visible : Visibility.Collapsed;
        BlockedActionButton.Visibility = Mode == CardMode.Blocked ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateMenuTexts()
    {
        if (_favoriteMenuItem != null)
        {
            _favoriteMenuItem.Text = IsFavorite ? "Entfavorisieren" : "Favorisieren";
            if (_favoriteMenuItem.Icon is FontIcon favIcon)
            {
                // Filled heart when favorited, outline when not
                favIcon.Glyph = IsFavorite ? "\uEB52" : "\uEB51";
            }
        }

        if (_blockMenuItem != null)
        {
            _blockMenuItem.Text = IsBlocked ? "Entblockieren" : "Blockieren";
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

        // Show HAUS, GRUND, and ZV badges
        TypeBadge.Visibility = property.Type switch
        {
            PropertyType.House => Visibility.Visible,
            PropertyType.Land => Visibility.Visible,
            PropertyType.Foreclosure => Visibility.Visible,
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

    private void OnCardTapped(object sender, TappedRoutedEventArgs e)
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

    private void OnPrevImageTapped(object sender, TappedRoutedEventArgs e)
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

    private void OnNextImageTapped(object sender, TappedRoutedEventArgs e)
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

    private void OnMoreOptionsTapped(object sender, TappedRoutedEventArgs e)
    {
        // Verhindere Navigation zur Detail-Seite
        e.Handled = true;
    }

    private void OnBlockClick(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[PropertyCard] OnBlockClick called for: {Property?.Title}");
        if (Property != null)
        {
            System.Diagnostics.Debug.WriteLine($"[PropertyCard] Invoking PropertyBlocked event");
            PropertyBlocked?.Invoke(this, Property);
        }
    }

    private void OnFavoriteClick(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[PropertyCard] OnFavoriteClick called for: {Property?.Title}");
        if (Property != null)
        {
            PropertyFavorited?.Invoke(this, Property);
        }
    }

    private void OnActionButtonTapped(object sender, TappedRoutedEventArgs e)
    {
        // Verhindere Navigation zur Detail-Seite
        e.Handled = true;
    }

    private void OnFavoriteActionClick(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[PropertyCard] OnFavoriteActionClick (remove from favorites) called for: {Property?.Title}");
        if (Property != null)
        {
            PropertyFavorited?.Invoke(this, Property);
        }
    }

    private void OnBlockedActionClick(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[PropertyCard] OnBlockedActionClick (unblock) called for: {Property?.Title}");
        if (Property != null)
        {
            PropertyBlocked?.Invoke(this, Property);
        }
    }
}
