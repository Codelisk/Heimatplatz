using ICommand = System.Windows.Input.ICommand;
using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Features.Properties.Models;

namespace Heimatplatz.Maui.Features.Properties.Controls;

/// <summary>
/// PropertyCard - Zeigt eine Immobilie in der Listenansicht
/// (Bild, Titel, Ort, Preis, Typ-Badge, Favoriten-/Blockieren-Buttons)
/// </summary>
public partial class PropertyCard : ContentView
{
    public PropertyCard()
    {
        InitializeComponent();
    }

    // Zuletzt gesetzte Bild-URL, um beim Card-Recycling unnoetige Reloads zu vermeiden
    private string? _currentImageUrl;

    /// <summary>
    /// Die anzuzeigende Immobilie
    /// </summary>
    public static readonly BindableProperty PropertyProperty =
        BindableProperty.Create(
            nameof(Property),
            typeof(PropertyListItemDto),
            typeof(PropertyCard),
            propertyChanged: (b, _, n) =>
            {
                if (b is PropertyCard card && n is PropertyListItemDto property)
                    card.UpdateDisplay(property);
            });

    public PropertyListItemDto? Property
    {
        get => (PropertyListItemDto?)GetValue(PropertyProperty);
        set => SetValue(PropertyProperty, value);
    }

    /// <summary>
    /// Ob diese Immobilie favorisiert ist (steuert das Herz-Icon)
    /// </summary>
    public static readonly BindableProperty IsFavoriteProperty =
        BindableProperty.Create(
            nameof(IsFavorite),
            typeof(bool),
            typeof(PropertyCard),
            false,
            propertyChanged: (b, _, _) => ((PropertyCard)b).UpdateFavoriteGlyph());

    public bool IsFavorite
    {
        get => (bool)GetValue(IsFavoriteProperty);
        set => SetValue(IsFavoriteProperty, value);
    }

    /// <summary>
    /// Ob der Benutzer eingeloggt ist (steuert Sichtbarkeit der Aktions-Buttons im Default-Modus)
    /// </summary>
    public static readonly BindableProperty IsAuthenticatedProperty =
        BindableProperty.Create(
            nameof(IsAuthenticated),
            typeof(bool),
            typeof(PropertyCard),
            false,
            propertyChanged: (b, _, _) => ((PropertyCard)b).UpdateActionButtonVisibility());

    public bool IsAuthenticated
    {
        get => (bool)GetValue(IsAuthenticatedProperty);
        set => SetValue(IsAuthenticatedProperty, value);
    }

    /// <summary>
    /// Der Anzeigemodus der Card (Default, Favorite, Blocked, Owner)
    /// </summary>
    public static readonly BindableProperty ModeProperty =
        BindableProperty.Create(
            nameof(Mode),
            typeof(CardMode),
            typeof(PropertyCard),
            CardMode.Default,
            propertyChanged: (b, _, _) => ((PropertyCard)b).UpdateActionButtonVisibility());

    public CardMode Mode
    {
        get => (CardMode)GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    /// <summary>
    /// Command das beim Klick auf die Card ausgefuehrt wird
    /// </summary>
    public static readonly BindableProperty CardClickedCommandProperty =
        BindableProperty.Create(nameof(CardClickedCommand), typeof(ICommand), typeof(PropertyCard));

    public ICommand? CardClickedCommand
    {
        get => (ICommand?)GetValue(CardClickedCommandProperty);
        set => SetValue(CardClickedCommandProperty, value);
    }

    /// <summary>
    /// Command zum Favorisieren/Entfavorisieren
    /// </summary>
    public static readonly BindableProperty FavoriteCommandProperty =
        BindableProperty.Create(nameof(FavoriteCommand), typeof(ICommand), typeof(PropertyCard));

    public ICommand? FavoriteCommand
    {
        get => (ICommand?)GetValue(FavoriteCommandProperty);
        set => SetValue(FavoriteCommandProperty, value);
    }

    /// <summary>
    /// Command zum Blockieren/Entblockieren
    /// </summary>
    public static readonly BindableProperty BlockCommandProperty =
        BindableProperty.Create(nameof(BlockCommand), typeof(ICommand), typeof(PropertyCard));

    public ICommand? BlockCommand
    {
        get => (ICommand?)GetValue(BlockCommandProperty);
        set => SetValue(BlockCommandProperty, value);
    }

    /// <summary>
    /// Command zum Loeschen der eigenen Immobilie (Owner-Modus)
    /// </summary>
    public static readonly BindableProperty DeleteCommandProperty =
        BindableProperty.Create(nameof(DeleteCommand), typeof(ICommand), typeof(PropertyCard));

    public ICommand? DeleteCommand
    {
        get => (ICommand?)GetValue(DeleteCommandProperty);
        set => SetValue(DeleteCommandProperty, value);
    }

    private void UpdateDisplay(PropertyListItemDto property)
    {
        // Preis (kompakt) - ausblenden wenn 0
        var price = (decimal)property.Price;
        if (price > 0)
        {
            PriceOverlay.IsVisible = true;
            PriceText.Text = FormatPrice(price);
        }
        else
        {
            PriceOverlay.IsVisible = false;
        }

        // Ort und Adresse (eine Zeile; Trenner nur wenn beides vorhanden)
        OrtText.Text = property.City;
        AddressText.Text = property.Address;
        var hasAddress = !string.IsNullOrWhiteSpace(property.Address);
        AddressText.IsVisible = hasAddress;
        OrtAddressSeparator.IsVisible = hasAddress && !string.IsNullOrWhiteSpace(property.City);

        // Titel
        TitleText.Text = property.Title;

        // Typ-Badge Text und Farbe
        TypeBadgeText.Text = property.Type switch
        {
            PropertyType.House => "HAUS",
            PropertyType.Land => "GRUND",
            PropertyType.Foreclosure => "ZV",
            _ => "IMM"
        };

        // Typ-Farben (wie Web): ZV = Signal-Rot, Grund = Gruen, Haus = Blau,
        // Rest warmes Glas - alle mit weisser Schrift, themenunabhaengig
        TypeBadge.BackgroundColor = property.Type switch
        {
            PropertyType.Foreclosure => Color.FromArgb("#DE2A2F"),
            PropertyType.Land => Color.FromArgb("#33854A"),
            PropertyType.House => Color.FromArgb("#2F6E9E"),
            _ => Color.FromArgb("#66171310")
        };

        TypeBadge.IsVisible = property.Type is PropertyType.House or PropertyType.Land or PropertyType.Foreclosure;

        // Grundstuecksflaeche - ausblenden wenn nicht vorhanden
        if (property.PlotAreaM2.HasValue)
        {
            GrundstueckPanel.IsVisible = true;
            GrundstueckText.Text = $"{PropertyDisplay.Number(property.PlotAreaM2.Value)} m² Grund";
        }
        else
        {
            GrundstueckPanel.IsVisible = false;
        }

        // Wohnflaeche
        if (property.LivingAreaM2.HasValue)
        {
            WohnflaechePanel.IsVisible = true;
            WohnflaecheText.Text = $"{PropertyDisplay.Number(property.LivingAreaM2.Value)} m² Wfl";
        }
        else
        {
            WohnflaechePanel.IsVisible = false;
        }

        // Zimmer
        if (property.Rooms.HasValue)
        {
            RoomsPanel.IsVisible = true;
            RoomsText.Text = $"{property.Rooms.Value} Zi";
        }
        else
        {
            RoomsPanel.IsVisible = false;
        }

        // Anbieter (kompakt)
        SellerBadgeText.Text = property.SellerType switch
        {
            SellerType.Private => "Privat",
            _ => property.SellerName
        };

        // Eingestellt am
        CreatedAtText.Text = property.CreatedAt.ToString("dd.MM.yyyy");

        // Bild (erstes Bild) und Bild-Counter
        var imageUrls = property.ImageUrls?.Where(url => !string.IsNullOrEmpty(url)).ToList();
        var newImageUrl = imageUrls?.Count > 0 ? imageUrls[0] : null;
        if (newImageUrl != _currentImageUrl)
        {
            _currentImageUrl = newImageUrl;
            // Beim Card-Recycling zuerst leeren: WinUI zeigt sonst das alte Foto
            // weiter, bis das neue geladen ist (falsches Bild auf falscher Immobilie)
            MainImage.Source = null;
            if (newImageUrl != null)
                MainImage.Source = ImageSource.FromUri(new Uri(newImageUrl));
        }
        MainImage.IsVisible = newImageUrl != null;

        if (imageUrls?.Count > 1)
        {
            ImageCounterBadge.IsVisible = true;
            ImageCounterText.Text = $"1/{imageUrls.Count}";
        }
        else
        {
            ImageCounterBadge.IsVisible = false;
        }

        UpdateActionButtonVisibility();
        UpdateFavoriteGlyph();
    }

    private void UpdateActionButtonVisibility()
    {
        FavoriteButton.IsVisible = Mode == CardMode.Default && IsAuthenticated;
        BlockButton.IsVisible = Mode == CardMode.Default && IsAuthenticated;
        FavoriteActionButton.IsVisible = Mode == CardMode.Favorite;
        BlockedActionButton.IsVisible = Mode == CardMode.Blocked;
        OwnerDeleteButton.IsVisible = Mode == CardMode.Owner;
    }

    private void UpdateFavoriteGlyph()
    {
        // Gemerkt = gefuellter roter Button statt Glas (wie Web [aria-pressed=true])
        FavoriteButton.Text = IsFavorite ? "♥" : "♡";
        FavoriteButton.TextColor = Colors.White;
        FavoriteButton.BackgroundColor = IsFavorite
            ? Color.FromArgb("#DE2A2F")
            : Color.FromArgb("#66171310");
    }

    private static string FormatPrice(decimal price)
    {
        // Voller Preis statt Kompaktform ("alle Infos auf einen Blick");
        // nur Millionenbetraege werden verdichtet
        if (price >= 1_000_000)
            return string.Format(PropertyDisplay.Culture, "{0:0.##} Mio €", price / 1_000_000);
        return PropertyDisplay.Price(price);
    }

    private void OnCardTapped(object? sender, TappedEventArgs e)
    {
        if (Property != null && CardClickedCommand?.CanExecute(Property) == true)
            CardClickedCommand.Execute(Property);
    }

    private void OnFavoriteClicked(object? sender, EventArgs e)
    {
        if (Property != null && FavoriteCommand?.CanExecute(Property) == true)
            FavoriteCommand.Execute(Property);
    }

    private void OnBlockClicked(object? sender, EventArgs e)
    {
        if (Property != null && BlockCommand?.CanExecute(Property) == true)
            BlockCommand.Execute(Property);
    }

    private void OnOwnerDeleteClicked(object? sender, EventArgs e)
    {
        if (Property != null && DeleteCommand?.CanExecute(Property) == true)
            DeleteCommand.Execute(Property);
    }
}
