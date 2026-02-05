using WinInput = System.Windows.Input;
using Heimatplatz.Features.Properties.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using UnoFramework.Contracts.Application;

namespace Heimatplatz.Features.Properties.Controls;

/// <summary>
/// PropertyCard - Zeigt eine Immobilie in der Listenansicht
/// </summary>
public sealed partial class PropertyCard : UserControl
{
    private MenuFlyoutItem? _favoriteMenuItem;
    private MenuFlyoutItem? _blockMenuItem;
    private List<string>? _currentImageUrls;

    public PropertyCard()
    {
        this.InitializeComponent();
        this.Loaded += OnLoaded;

        // Update menu texts when flyout opens (ensures correct state)
        if (MoreOptionsButton.Flyout is MenuFlyout menuFlyout)
        {
            menuFlyout.Opening += OnMenuFlyoutOpening;
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        AcquireMenuItemReferences();
        UpdateMenuTexts();
    }

    private void OnMenuFlyoutOpening(object? sender, object e)
    {
        AcquireMenuItemReferences();
        RefreshStatusFromService();
        UpdateMenuTexts();
    }

    private void AcquireMenuItemReferences()
    {
        if (_favoriteMenuItem != null && _blockMenuItem != null)
            return;

        if (MoreOptionsButton.Flyout is not MenuFlyout menuFlyout)
            return;

        foreach (var item in menuFlyout.Items)
        {
            if (item is not MenuFlyoutItem menuItem)
                continue;

            if (menuItem.Text.Contains("avorisieren"))
                _favoriteMenuItem = menuItem;
            else if (menuItem.Text.Contains("lockieren"))
                _blockMenuItem = menuItem;
        }
    }

    private void RefreshStatusFromService()
    {
        if (Property == null)
            return;

        if (Application.Current is not IApplicationWithServices { Services: not null } appWithServices)
            return;

        var statusService = appWithServices.Services.GetService<IPropertyStatusService>();
        if (statusService == null)
            return;

        IsFavorite = statusService.IsFavorite(Property.Id);
        IsBlocked = statusService.IsBlocked(Property.Id);
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
    /// Event wenn die eigene Immobilie gelöscht werden soll (Owner-Modus)
    /// </summary>
    public event EventHandler<PropertyListItemDto>? PropertyDeleted;

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
    /// Ob der Benutzer eingeloggt ist (steuert Sichtbarkeit des 3-Punkte-Menüs)
    /// </summary>
    public static readonly DependencyProperty IsAuthenticatedProperty =
        DependencyProperty.Register(
            nameof(IsAuthenticated),
            typeof(bool),
            typeof(PropertyCard),
            new PropertyMetadata(false, OnIsAuthenticatedChanged));

    public bool IsAuthenticated
    {
        get => (bool)GetValue(IsAuthenticatedProperty);
        set => SetValue(IsAuthenticatedProperty, value);
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

    /// <summary>
    /// Command das beim Klick auf die Card ausgeführt wird (Alternative zu CardClicked Event)
    /// </summary>
    public static readonly DependencyProperty CardClickedCommandProperty =
        DependencyProperty.Register(
            nameof(CardClickedCommand),
            typeof(WinInput.ICommand),
            typeof(PropertyCard),
            new PropertyMetadata(null));

    public WinInput.ICommand? CardClickedCommand
    {
        get => (WinInput.ICommand?)GetValue(CardClickedCommandProperty);
        set => SetValue(CardClickedCommandProperty, value);
    }

    /// <summary>
    /// Command das beim Löschen der Immobilie ausgeführt wird (Alternative zu PropertyDeleted Event)
    /// </summary>
    public static readonly DependencyProperty DeleteCommandProperty =
        DependencyProperty.Register(
            nameof(DeleteCommand),
            typeof(WinInput.ICommand),
            typeof(PropertyCard),
            new PropertyMetadata(null));

    public WinInput.ICommand? DeleteCommand
    {
        get => (WinInput.ICommand?)GetValue(DeleteCommandProperty);
        set => SetValue(DeleteCommandProperty, value);
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

    private static void OnIsAuthenticatedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PropertyCard card)
        {
            card.UpdateActionButtonVisibility();
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
        // Null-Checks nötig, da diese Methode von DependencyProperty-Callbacks aufgerufen wird,
        // bevor InitializeComponent() abgeschlossen ist
        if (MoreOptionsButton != null)
            MoreOptionsButton.Visibility = Mode == CardMode.Default && IsAuthenticated ? Visibility.Visible : Visibility.Collapsed;
        if (FavoriteActionButton != null)
            FavoriteActionButton.Visibility = Mode == CardMode.Favorite ? Visibility.Visible : Visibility.Collapsed;
        if (BlockedActionButton != null)
            BlockedActionButton.Visibility = Mode == CardMode.Blocked ? Visibility.Visible : Visibility.Collapsed;
        if (OwnerDeleteButton != null)
            OwnerDeleteButton.Visibility = Mode == CardMode.Owner ? Visibility.Visible : Visibility.Collapsed;
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

        // Type badge text and color - using theme resources for consistency
        TypeBadgeText.Text = property.Type switch
        {
            PropertyType.House => "HAUS",
            PropertyType.Land => "GRUND",
            PropertyType.Foreclosure => "ZV",
            _ => "IMM"
        };

        // Type-specific colors from design system
        TypeBadge.Background = property.Type switch
        {
            PropertyType.House => GetThemeResourceOrFallback("HausBrush", "#2D5F4C"),
            PropertyType.Land => GetThemeResourceOrFallback("GrundstueckBrush", "#5D8A66"),
            PropertyType.Foreclosure => GetThemeResourceOrFallback("ZwangsversteigerungBrush", "#B22222"),
            _ => (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AccentBrush"]
        };
        TypeBadgeText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White);

        // Show type badge for House, Land, and Foreclosure
        TypeBadge.Visibility = property.Type is PropertyType.House or PropertyType.Land or PropertyType.Foreclosure
            ? Visibility.Visible
            : Visibility.Collapsed;

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
        SellerBadgeText.Text = property.SellerType switch
        {
            SellerType.Private => "Privat",
            SellerType.Portal => $"Portal: {property.SellerName}",
            _ => property.SellerName
        };

        // Created date (relative)
        CreatedAtText.Text = FormatRelativeDate(property.CreatedAt);

        // Load images (FlipView for swipe)
        // Detach previous handler to prevent accumulation
        ImageFlipView.SelectionChanged -= OnImageFlipViewSelectionChanged;

        if (property.ImageUrls?.Count > 0)
        {
            var imageUrls = property.ImageUrls.Where(url => !string.IsNullOrEmpty(url)).ToList();
            _currentImageUrls = imageUrls;
            ImageFlipView.ItemsSource = imageUrls;

            // Show counter and arrows when multiple images
            if (imageUrls.Count > 1)
            {
                ImageCounterBadge.Visibility = Visibility.Visible;
                ImageCounterText.Text = $"1/{imageUrls.Count}";

                // Show navigation arrows
                PrevImageButton.Visibility = Visibility.Visible;
                NextImageButton.Visibility = Visibility.Visible;

                ImageFlipView.SelectionChanged += OnImageFlipViewSelectionChanged;
            }
            else
            {
                ImageCounterBadge.Visibility = Visibility.Collapsed;
                PrevImageButton.Visibility = Visibility.Collapsed;
                NextImageButton.Visibility = Visibility.Collapsed;
            }
        }
        else
        {
            _currentImageUrls = null;
            ImageFlipView.ItemsSource = null;
            ImageCounterBadge.Visibility = Visibility.Collapsed;
            PrevImageButton.Visibility = Visibility.Collapsed;
            NextImageButton.Visibility = Visibility.Collapsed;
        }
    }

    private void OnImageFlipViewSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_currentImageUrls != null)
        {
            ImageCounterText.Text = $"{ImageFlipView.SelectedIndex + 1}/{_currentImageUrls.Count}";
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

    private static string FormatRelativeDate(DateTime createdAt)
    {
        return createdAt.ToString("dd.MM.yyyy");
    }

    private static Microsoft.UI.Xaml.Media.Brush GetThemeResourceOrFallback(string resourceKey, string fallbackColor)
    {
        if (Application.Current.Resources.TryGetValue(resourceKey, out var resource) && resource is Microsoft.UI.Xaml.Media.Brush brush)
        {
            return brush;
        }
        return new Microsoft.UI.Xaml.Media.SolidColorBrush(ParseColor(fallbackColor));
    }

    private static Windows.UI.Color ParseColor(string hex)
    {
        hex = hex.TrimStart('#');
        var r = byte.Parse(hex[..2], System.Globalization.NumberStyles.HexNumber);
        var g = byte.Parse(hex[2..4], System.Globalization.NumberStyles.HexNumber);
        var b = byte.Parse(hex[4..6], System.Globalization.NumberStyles.HexNumber);
        return Windows.UI.Color.FromArgb(255, r, g, b);
    }

    private void OnCardTapped(object sender, TappedRoutedEventArgs e)
    {
        if (Property != null)
        {
            // Invoke event (for code-behind handlers)
            CardClicked?.Invoke(this, Property);

            // Invoke command (for MVVM binding)
            if (CardClickedCommand?.CanExecute(Property) == true)
            {
                CardClickedCommand.Execute(Property);
            }
        }
    }

    private void OnPrevImageClick(object sender, RoutedEventArgs e)
    {
        var count = ImageFlipView.Items.Count;
        if (count == 0)
            return;

        // Wrap to last image when at beginning
        ImageFlipView.SelectedIndex = ImageFlipView.SelectedIndex > 0
            ? ImageFlipView.SelectedIndex - 1
            : count - 1;
    }

    private void OnPrevImageTapped(object sender, TappedRoutedEventArgs e)
    {
        e.Handled = true;
    }

    private void OnNextImageClick(object sender, RoutedEventArgs e)
    {
        var count = ImageFlipView.Items.Count;
        if (count == 0)
            return;

        // Wrap to first image when at end
        ImageFlipView.SelectedIndex = ImageFlipView.SelectedIndex < count - 1
            ? ImageFlipView.SelectedIndex + 1
            : 0;
    }

    private void OnNextImageTapped(object sender, TappedRoutedEventArgs e)
    {
        e.Handled = true;
    }


    private void OnMoreOptionsTapped(object sender, TappedRoutedEventArgs e)
    {
        // Verhindere Navigation zur Detail-Seite
        e.Handled = true;
    }

    private void OnBlockClick(object sender, RoutedEventArgs e)
    {
        if (Property != null)
        {
            PropertyBlocked?.Invoke(this, Property);
        }
    }

    private void OnFavoriteClick(object sender, RoutedEventArgs e)
    {
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
        if (Property != null)
        {
            PropertyFavorited?.Invoke(this, Property);
        }
    }

    private void OnBlockedActionClick(object sender, RoutedEventArgs e)
    {
        if (Property != null)
        {
            PropertyBlocked?.Invoke(this, Property);
        }
    }

    private void OnOwnerDeleteClick(object sender, RoutedEventArgs e)
    {
        if (Property != null)
        {
            // Invoke event (for code-behind handlers)
            PropertyDeleted?.Invoke(this, Property);

            // Invoke command (for MVVM binding)
            if (DeleteCommand?.CanExecute(Property) == true)
            {
                DeleteCommand.Execute(Property);
            }
        }
    }
}
