using ICommand = System.Windows.Input.ICommand;
using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Features.Properties.Models;
using Heimatplatz.Maui.Features.Properties.Services;
using Heimatplatz.Maui.Localization.Properties;
using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.Maui.Features.Properties.Controls;

/// <summary>
/// PropertyCard - Zeigt eine Immobilie in der Listenansicht
/// (Bild, Titel, Ort, Preis, Typ-Badge, Favoriten-/Blockieren-Buttons)
/// </summary>
public partial class PropertyCard : ContentView
{
    // Gemeinsame Icon-Quellen fuer alle recycelten Cards (FileImageSource ist nur
    // ein Deskriptor - jedes Image loest selbst auf, Teilen ist unkritisch)
    private static readonly ImageSource HeartOutlineIcon = ImageSource.FromFile("icon_heart_white.png");
    private static readonly ImageSource HeartFilledIcon = ImageSource.FromFile("icon_heart_filled.png");
    private static readonly ImageSource BlockGlyphIcon = ImageSource.FromFile("icon_block_white.png");
    private static readonly ImageSource CloseGlyphIcon = ImageSource.FromFile("icon_close_white.png");

    // Localized Kartentexte per Service-Locator - PropertyCard ist ein ContentView
    // ohne DI (gleiches Muster wie IPropertyStatusService in OnCardLoaded). Statisch
    // gecacht, damit recycelte Cards den Singleton nicht mehrfach aufloesen.
    private static PropertyCardStringsLocalized? _locCache;
    private static PropertyCardStringsLocalized Loc =>
        _locCache ??= IPlatformApplication.Current!.Services.GetRequiredService<PropertyCardStringsLocalized>();

    public PropertyCard()
    {
        InitializeComponent();

        // Statische Texte aus den Ressourcen (SemanticProperties sind nicht bindbar
        // ohne BindingContext-Verrenkungen - Code-Behind ist hier robuster)
        SemanticProperties.SetDescription(FavoriteButton, Loc.FavoriteActionDescription);
        SemanticProperties.SetDescription(BlockButton, Loc.BlockActionDescription);

        // Favoriten-Status kommt zentral aus dem PropertyStatusService (das Herz
        // aktualisiert sich damit sofort nach jedem Toggle). Abo nur solange die
        // Card im Visual Tree haengt - Cards werden in CollectionViews recycelt,
        // ein Ctor-Abo auf den Singleton wuerde leaken.
        Loaded += OnCardLoaded;
        Unloaded += OnCardUnloaded;

        // Wasserzeichen-Placeholder nach erfolgreichem Foto-Load ausblenden -
        // er wuerde sonst unter jedem Foto weiter mitgezeichnet (Overdraw)
        MainImage.PropertyChanged += OnMainImagePropertyChanged;
    }

    private void OnMainImagePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Image.IsLoading) && !MainImage.IsLoading && _currentImageUrl != null)
            PlaceholderPanel.IsVisible = false;
    }

    // Zuletzt gesetzte Bild-URL, um beim Card-Recycling unnoetige Reloads zu vermeiden
    private string? _currentImageUrl;

    private IPropertyStatusService? _statusService;

    private void OnCardLoaded(object? sender, EventArgs e)
    {
        _statusService ??= IPlatformApplication.Current?.Services.GetService<IPropertyStatusService>();
        if (_statusService != null)
        {
            // Erst ab-, dann anmelden: Loaded kann ohne dazwischenliegendes Unloaded
            // mehrfach feuern (Tab-Wechsel), doppelte Abos wuerden sich sonst stapeln
            _statusService.StatusChanged -= OnStatusChanged;
            _statusService.StatusChanged += OnStatusChanged;
        }

        SyncFavoriteState();
    }

    private void OnCardUnloaded(object? sender, EventArgs e)
    {
        if (_statusService != null)
            _statusService.StatusChanged -= OnStatusChanged;
    }

    private void OnStatusChanged(object? sender, EventArgs e)
    {
        // StatusChanged kann aus Mediator-/Hintergrund-Threads feuern
        MainThread.BeginInvokeOnMainThread(SyncFavoriteState);
    }

    private void SyncFavoriteState()
    {
        if (_statusService != null && Property != null)
            IsFavorite = _statusService.IsFavorite(Property.Id);
    }

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
            PropertyType.House => Loc.TypeBadgeHouse,
            PropertyType.Land => Loc.TypeBadgeLand,
            PropertyType.Foreclosure => Loc.TypeBadgeForeclosure,
            _ => Loc.TypeBadgeFallback
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
            GrundstueckText.Text = Loc.PlotAreaFormat(PropertyDisplay.Number(property.PlotAreaM2.Value));
        }
        else
        {
            GrundstueckPanel.IsVisible = false;
        }

        // Wohnflaeche
        if (property.LivingAreaM2.HasValue)
        {
            WohnflaechePanel.IsVisible = true;
            WohnflaecheText.Text = Loc.LivingAreaFormat(PropertyDisplay.Number(property.LivingAreaM2.Value));
        }
        else
        {
            WohnflaechePanel.IsVisible = false;
        }

        // Zimmer
        if (property.Rooms.HasValue)
        {
            RoomsPanel.IsVisible = true;
            RoomsText.Text = Loc.RoomsFormat(property.Rooms.Value);
        }
        else
        {
            RoomsPanel.IsVisible = false;
        }

        // Anbieter (kompakt)
        SellerBadgeText.Text = property.SellerType switch
        {
            SellerType.Private => Loc.SellerPrivate,
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
            // Wasserzeichen zeigen bis das neue Foto geladen ist (OnMainImagePropertyChanged
            // blendet es dann aus); bei gleicher URL (Recycling auf dieselbe Immobilie)
            // bleibt der Zustand unangetastet
            PlaceholderPanel.IsVisible = true;
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
        // Recycelte Card kann an eine andere Immobilie gebunden worden sein -
        // Favoriten-Status fuer die neue Id nachziehen
        SyncFavoriteState();
        UpdateFavoriteGlyph();
    }

    private void UpdateActionButtonVisibility()
    {
        // Zwei wiederverwendete Buttons fuer alle CardModes (statt fuenf, groesstenteils
        // unsichtbarer) - Sichtbarkeit und Icons je Mode nachziehen
        switch (Mode)
        {
            case CardMode.Favorite:
                FavoritePanel.IsVisible = true;
                BlockPanel.IsVisible = false;
                break;
            case CardMode.Blocked:
            case CardMode.Owner:
                FavoritePanel.IsVisible = false;
                BlockPanel.IsVisible = true;
                BlockIcon.Source = CloseGlyphIcon;
                break;
            default:
                FavoritePanel.IsVisible = IsAuthenticated;
                BlockPanel.IsVisible = IsAuthenticated;
                BlockIcon.Source = BlockGlyphIcon;
                break;
        }

        UpdateFavoriteGlyph();
    }

    private void UpdateFavoriteGlyph()
    {
        // Gemerkt = das Herz selbst fuellt sich rot, der Glas-Chip bleibt unveraendert.
        // Auf der Favoriten-Seite ist der Eintrag immer aktiv, also immer gefuellt.
        var filled = Mode == CardMode.Favorite || IsFavorite;
        FavoriteIcon.Source = filled ? HeartFilledIcon : HeartOutlineIcon;
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
        // Der Button dient je nach Mode dem Blockieren/Entblockieren oder Loeschen (Owner)
        var command = Mode == CardMode.Owner ? DeleteCommand : BlockCommand;
        if (Property != null && command?.CanExecute(Property) == true)
            command.Execute(Property);
    }
}
