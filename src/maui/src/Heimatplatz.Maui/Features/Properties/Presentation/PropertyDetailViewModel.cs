using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Core.Collections;
using Heimatplatz.Maui.Core.Media;
using Heimatplatz.Maui.Features.Auth;
using Heimatplatz.Maui.Features.Properties.Models;
using PropertyCondition = Heimatplatz.Maui.Features.Properties.Models.PropertyCondition;
using Heimatplatz.Maui.Features.Properties.Services;
using Heimatplatz.Maui.Features.Properties.Sync;
using Heimatplatz.Maui.Localization;
using Heimatplatz.Maui.Localization.Properties;
using Heimatplatz.Maui.Offline;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;
using Shiny.Mediator.Infrastructure;

namespace Heimatplatz.Maui.Features.Properties.Presentation;

/// <summary>
/// ViewModel fuer die PropertyDetailPage.
/// Laedt die Immobilie anhand der PropertyId via API (GetPropertyByIdHttpRequest).
/// </summary>
[ShellMap<PropertyDetailPage>("PropertyDetail")]
public partial class PropertyDetailViewModel : ObservableObject, IPageLifecycleAware
{
    private readonly IClipboardService _clipboardService;
    private readonly IShareService _shareService;
    private readonly IMediator _mediator;
    private readonly IAuthService _authService;
    private readonly IPropertyStatusService _propertyStatusService;
    private readonly IInternetService _internet;
    private readonly OfflineReadState _offlineReadState;
    private readonly PropertyImageCache _imageCache;
    private readonly Features.Debug.Services.IApiEndpointService _apiEndpoints;
    private readonly PropertyHandoffCache _handoffCache;
    private readonly PropertyDetailPreloader _detailPreloader;
    private readonly PropertyDetailImageResolver _imageResolver;
    private readonly DetailNavigationTrace _trace;
    private readonly ILogger<PropertyDetailViewModel> _logger;
    private readonly PropertyDetailStringsLocalized _loc;
    private readonly CommonStringsLocalized _commonLoc;

    /// <summary>Lokalisierte Texte fuer XAML-Bindings (Loc.Key)</summary>
    public PropertyDetailStringsLocalized Loc => _loc;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Navigationsparameter: Id der zu ladenden Immobilie
    /// </summary>
    [ShellProperty]
    public string PropertyId { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string? BusyMessage { get; set; }

    [ObservableProperty]
    public partial bool IsShowingCachedData { get; set; }

    [ObservableProperty]
    public partial PropertyDto? Property { get; set; }

    [ObservableProperty]
    public partial string Title { get; set; }

    [ObservableProperty]
    public partial string FormattedPrice { get; set; }

    [ObservableProperty]
    public partial string AddressText { get; set; }

    [ObservableProperty]
    public partial string ContactPersonText { get; set; }

    [ObservableProperty]
    public partial bool HasContactPerson { get; set; }

    [ObservableProperty]
    public partial string? CopyFeedback { get; set; }

    [ObservableProperty]
    public partial bool HasCopyFeedback { get; set; }

    [ObservableProperty]
    public partial bool IsAuthenticated { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FavoriteButtonText))]
    [NotifyPropertyChangedFor(nameof(FavoriteButtonIcon))]
    public partial bool IsFavorite { get; set; }

    /// <summary>True wenn die Immobilie nicht geladen werden konnte (Fehler oder geloescht)</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowContent))]
    public partial bool HasLoadError { get; set; }

    [ObservableProperty]
    public partial string? LoadErrorText { get; set; }

    /// <summary>Bilddatei des Fehlerzustands (Offline-Antenne bzw. Nicht-verfuegbar), ersetzt das fruehere Emoji</summary>
    [ObservableProperty]
    public partial string LoadErrorIcon { get; set; }

    [ObservableProperty]
    public partial string LoadErrorTitle { get; set; }

    /// <summary>True wenn ein erneuter Ladeversuch sinnvoll ist (nicht bei geloeschten Inseraten)</summary>
    [ObservableProperty]
    public partial bool CanRetryLoad { get; set; }

    /// <summary>Inhalt ausblenden solange der Fehlerzustand angezeigt wird</summary>
    public bool ShowContent => !HasLoadError;

    [ObservableProperty]
    public partial string TypeBadgeText { get; set; }

    [ObservableProperty]
    public partial Color TypeBadgeColor { get; set; }

    [ObservableProperty]
    public partial List<PropertyDetailSection> DetailSections { get; set; }

    /// <summary>Kernfakten als Kacheln direkt unter dem Kopf (auf einen Blick)</summary>
    [ObservableProperty]
    public partial List<StatTileItem> StatTiles { get; set; }

    [ObservableProperty]
    public partial bool HasStatTiles { get; set; }

    [ObservableProperty]
    public partial string? Description { get; set; }

    [ObservableProperty]
    public partial bool HasDescription { get; set; }

    [ObservableProperty]
    public partial bool IsBroker { get; set; }

    [ObservableProperty]
    public partial string? OriginalListingUrl { get; set; }

    [ObservableProperty]
    public partial bool HasOriginalListingUrl { get; set; }

    [ObservableProperty]
    public partial bool HasImages { get; set; }

    /// <summary>
    /// Angezeigte Bilder (Thumbnail bis die Vorschau lokal vorliegt). Feste Instanz mit
    /// In-Place-Updates: ein Austausch der Liste wuerde die CarouselView komplett neu
    /// aufbauen, jedes Foto neu laden lassen und die Position zuruecksetzen.
    /// </summary>
    public ObservableRangeCollection<string> ImageUrls { get; } = [];

    /// <summary>Volle Display-Varianten - ausschliesslich fuer den Vollbild-Viewer</summary>
    private List<string> _fullImageUrls = [];

    /// <summary>Vorschau-Varianten (1280px), die im Hintergrund nachgeladen werden</summary>
    private List<string> _previewImageUrls = [];

    private CancellationTokenSource? _imageUpgradeCts;

    [ObservableProperty]
    public partial string? SellerName { get; set; }

    [ObservableProperty]
    public partial List<ContactDisplayItem> Contacts { get; set; }

    [ObservableProperty]
    public partial bool HasContacts { get; set; }

    /// <summary>True wenn der Kontakt-Footer angezeigt wird (Anbieter oder Kontakte vorhanden)</summary>
    [ObservableProperty]
    public partial bool HasContactFooter { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ContactExpandIcon))]
    public partial bool IsContactExpanded { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ImageCounterText))]
    [NotifyPropertyChangedFor(nameof(CurrentImageUrl))]
    [NotifyPropertyChangedFor(nameof(CurrentFullImageUrl))]
    public partial int CurrentImagePosition { get; set; }

    /// <summary>
    /// Chevron: nach oben wenn zugeklappt (= aufklappen), nach unten wenn aufgeklappt (= zuklappen)
    /// </summary>
    public string ContactExpandIcon => IsContactExpanded ? "▼" : "▲";

    /// <summary>
    /// Bild-Zaehler fuer das Carousel, z.B. "2 / 7"
    /// </summary>
    public string ImageCounterText => ImageUrls.Count > 0
        ? $"{CurrentImagePosition + 1} / {ImageUrls.Count}"
        : string.Empty;

    /// <summary>
    /// Aktuelles Bild fuer den Windows-Bildviewer (Einzelbild statt CarouselView)
    /// </summary>
    public string? CurrentImageUrl => CurrentImagePosition >= 0 && CurrentImagePosition < ImageUrls.Count
        ? ImageUrls[CurrentImagePosition]
        : null;

    /// <summary>
    /// Bild fuer den Vollbild-Viewer: die volle Aufloesung, sobald sie lokal vorliegt,
    /// bis dahin die bereits sichtbare Vorschau. Der Viewer bekommt bewusst immer nur
    /// lokale Dateien - eine entfernte URL laedt er unter WinUI nicht.
    /// </summary>
    public string? CurrentFullImageUrl
    {
        get
        {
            if (CurrentImagePosition >= 0 && CurrentImagePosition < _fullImageUrls.Count)
            {
                var full = _fullImageUrls[CurrentImagePosition];
                var cached = _imageCache.GetCachedOrOriginal(full);
                if (!string.Equals(cached, full, StringComparison.Ordinal))
                    return cached;
            }

            return CurrentImageUrl;
        }
    }

    /// <summary>True bei mehr als einem Bild (blendet die Pfeile im Windows-Bildviewer ein)</summary>
    public bool HasMultipleImages => ImageUrls.Count > 1;

    private void OnImagesChanged()
    {
        OnPropertyChanged(nameof(ImageCounterText));
        OnPropertyChanged(nameof(CurrentImageUrl));
        OnPropertyChanged(nameof(CurrentFullImageUrl));
        OnPropertyChanged(nameof(HasMultipleImages));
        OnPropertyChanged(nameof(ShowViewerNavigation));
    }

    /// <summary>True solange der Vollbild-Bildviewer (Lightbox) offen ist</summary>
    [ObservableProperty]
    public partial bool IsImageViewerOpen { get; set; }

    /// <summary>Kontakt-Footer ausblenden solange die Lightbox offen ist (liegt auf WinUI sonst ueber dem Backdrop)</summary>
    public bool ShowContactFooter => HasContactFooter && !IsImageViewerOpen;

    /// <summary>Pfeile im Vollbild-Viewer: nur offen UND mehrere Bilder (kombiniert, da WinUI verschachtelte IsVisible-Bindings nicht malt)</summary>
    public bool ShowViewerNavigation => IsImageViewerOpen && HasMultipleImages;

    partial void OnIsImageViewerOpenChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowContactFooter));
        OnPropertyChanged(nameof(ShowViewerNavigation));

        if (value)
            EnsureFullImageCached();
    }

    partial void OnCurrentImagePositionChanged(int value)
    {
        if (IsImageViewerOpen)
            EnsureFullImageCached();
    }

    private CancellationTokenSource? _fullImageCts;

    /// <summary>
    /// Holt die volle Aufloesung des gerade gezeigten Bildes in den Bild-Cache. Erst
    /// danach kann <see cref="CurrentFullImageUrl"/> sie liefern; bis dahin bleibt die
    /// Vorschau stehen. Nur auf Anforderung (geoeffneter Viewer) - die vollen Dateien
    /// sind bis zu 2560px breit und werden fuer die Seite selbst nie gebraucht.
    /// </summary>
    private void EnsureFullImageCached()
    {
        var index = CurrentImagePosition;
        if (index < 0 || index >= _fullImageUrls.Count)
            return;

        var url = _fullImageUrls[index];
        if (!string.Equals(_imageCache.GetCachedOrOriginal(url), url, StringComparison.Ordinal))
            return; // liegt bereits lokal

        _fullImageCts?.Cancel();
        _fullImageCts?.Dispose();
        var cts = new CancellationTokenSource();
        _fullImageCts = cts;

        _ = Task.Run(async () =>
        {
            try
            {
                await _imageCache.GetOrDownloadAsync(url, cts.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "[PropertyDetail] Vollbild konnte nicht geladen werden");
                return;
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (!cts.IsCancellationRequested && CurrentImagePosition == index)
                    OnPropertyChanged(nameof(CurrentFullImageUrl));
            });
        });
    }

    partial void OnHasContactFooterChanged(bool value)
        => OnPropertyChanged(nameof(ShowContactFooter));

    /// <summary>
    /// Text fuer den Favoriten-Button je nach Status
    /// </summary>
    public string FavoriteButtonText => IsFavorite ? _loc.FavoriteSaved : _loc.FavoriteSave;

    /// <summary>
    /// Icon fuer den Favoriten-Button (gefuelltes/leeres Herz)
    /// </summary>
    public string FavoriteButtonIcon => IsFavorite ? "♥" : "♡";

    public PropertyDetailViewModel(
        IClipboardService clipboardService,
        IShareService shareService,
        IMediator mediator,
        IAuthService authService,
        IPropertyStatusService propertyStatusService,
        IInternetService internet,
        OfflineReadState offlineReadState,
        PropertyImageCache imageCache,
        PropertyHandoffCache handoffCache,
        PropertyDetailPreloader detailPreloader,
        PropertyDetailImageResolver imageResolver,
        DetailNavigationTrace trace,
        Features.Debug.Services.IApiEndpointService apiEndpoints,
        ILogger<PropertyDetailViewModel> logger,
        PropertyDetailStringsLocalized loc,
        CommonStringsLocalized commonLoc)
    {
        _handoffCache = handoffCache;
        _detailPreloader = detailPreloader;
        _imageResolver = imageResolver;
        _trace = trace;
        _clipboardService = clipboardService;
        _shareService = shareService;
        _mediator = mediator;
        _authService = authService;
        _propertyStatusService = propertyStatusService;
        _internet = internet;
        _offlineReadState = offlineReadState;
        _imageCache = imageCache;
        _apiEndpoints = apiEndpoints;
        _logger = logger;
        _loc = loc;
        _commonLoc = commonLoc;

        Title = loc.FallbackTitle;
        LoadErrorIcon = string.Empty;
        LoadErrorTitle = string.Empty;
        FormattedPrice = string.Empty;
        AddressText = string.Empty;
        ContactPersonText = string.Empty;
        TypeBadgeText = string.Empty;
        TypeBadgeColor = Colors.Gray;
        DetailSections = [];
        StatTiles = [];
        Contacts = [];
        IsAuthenticated = authService.IsAuthenticated;
    }

    #region IPageLifecycleAware

    private IDisposable? _syncSubscription;

    public void OnAppearing()
    {
        // Hintergrund-Refreshes melden Serverausfaelle erst NACH dem synchronen
        // Cache-Hit - den Hinweis "zwischengespeicherte Daten" live nachziehen.
        _offlineReadState.Changed += OnOfflineReadStateChanged;

        if (Guid.TryParse(PropertyId, out var id))
        {
            _trace.Mark(id, "Seite sichtbar");

            // Vorschau aus den Listendaten der angetippten Karte: Kopf und erstes Foto
            // stehen damit im ersten Frame, ohne auf den Detail-Request zu warten
            ApplyHandoff(id);
            _trace.Mark(id, "Vorschau gezeichnet");

            _ = LoadPropertyAsync(id);

            // Delta-Sync: betrifft eine Aenderung/Loeschung die angezeigte Immobilie,
            // neu laden - der Sync hat den Detail-Cache bereits aktualisiert bzw. entfernt
            // (bei geloeschten Immobilien erscheint der "nicht mehr verfuegbar"-Zustand)
            _syncSubscription ??= _mediator.Subscribe<PropertyDataSyncedEvent>((evt, _, _) =>
            {
                var affected = evt.FullResync ||
                               evt.DeletedIds.Contains(id) ||
                               evt.ChangedProperties.Any(p => p.Id == id);
                if (affected)
                {
                    MainThread.BeginInvokeOnMainThread(() => _ = LoadPropertyAsync(id));
                }
                return Task.CompletedTask;
            });
        }
        else
        {
            _logger.LogWarning("[PropertyDetail] Invalid PropertyId: {PropertyId}", PropertyId);
        }
    }

    public void OnDisappearing()
    {
        _offlineReadState.Changed -= OnOfflineReadStateChanged;
        _syncSubscription?.Dispose();
        _syncSubscription = null;
        _onlineWaitCts?.Cancel();
        _onlineWaitCts = null;
        _imageUpgradeCts?.Cancel();
        _imageUpgradeCts = null;
    }

    private void OnOfflineReadStateChanged(object? sender, EventArgs e) =>
        MainThread.BeginInvokeOnMainThread(() =>
            IsShowingCachedData = _offlineReadState.IsBackendUnavailable);

    #endregion

    private CancellationTokenSource? _onlineWaitCts;

    /// <summary>True sobald die Vorschau aus den Listendaten auf der Seite steht</summary>
    private bool _hasHandoffPreview;

    /// <summary>
    /// Uebernimmt die Listendaten der angetippten Karte als Sofort-Anzeige. Alle Werte
    /// stammen aus derselben Quelle wie die Karte und werden vom Detail-Ergebnis
    /// ersetzt, sobald es vorliegt.
    /// </summary>
    private void ApplyHandoff(Guid propertyId)
    {
        var item = _handoffCache.Get(propertyId);
        if (item == null)
            return;

        _hasHandoffPreview = true;

        Title = PropertyDisplay.DisplayTitle(item.Title) is { Length: > 0 } title
            ? title
            : _loc.FallbackTitle;
        FormattedPrice = item.Price > 0 ? PropertyDisplay.Price((decimal)item.Price) : string.Empty;
        AddressText = $"{item.Address}, {item.PostalCode} {item.City}";

        TypeBadgeText = GetTypeBadgeText(item.Type);
        TypeBadgeColor = GetTypeBadgeColor(item.Type);

        // Anbieterzeile sofort setzen, damit der Kontakt-Footer nicht nachtraeglich
        // aufpoppt; die Kontaktdetails darin kommen mit den Detaildaten
        SellerName = item.SellerName;
        HasContactFooter = !string.IsNullOrWhiteSpace(item.SellerName);

        StatTiles = BuildHandoffStatTiles(item);
        HasStatTiles = StatTiles.Count > 0;

        // Die Listen-URLs sind die Thumbnails, die die Karte bereits geladen hat -
        // GetCachedOrOriginal loest sie auf den lokalen Dateipfad auf (kein Download)
        CurrentImagePosition = 0;
        SetImages(item.ImageUrls?
            .Where(url => !string.IsNullOrEmpty(url))
            .Select(_imageCache.GetCachedOrOriginal)
            .ToList() ?? []);
    }

    /// <summary>
    /// Setzt die angezeigten Bilder. Bleibt die Anzahl gleich (Vorschau aus den
    /// Listendaten -> Detaildaten derselben Immobilie), werden nur die geaenderten
    /// Positionen ersetzt - die CarouselView behaelt dadurch ihre Items und ihre
    /// Position, statt alles neu zu laden.
    /// </summary>
    private void SetImages(IReadOnlyList<string> urls)
    {
        if (ImageUrls.Count == urls.Count)
        {
            for (var i = 0; i < urls.Count; i++)
                PatchImage(i, urls[i]);
        }
        else
        {
            // ReplaceRange (eine Reset-Notification) statt Clear + N Adds: die
            // CarouselView-Handler von iOS fassen die Einzel-Notifications zu einem
            // performBatchUpdates zusammen und rechnen sie gegen die bereits gefuellte
            // Liste - "Invalid update: invalid number of items in section 0" beendet
            // die App. Genau dieser Fall trat beim Wechsel von der Handoff-Vorschau
            // (ein Listenfoto) auf die geladenen Detailfotos auf.
            ImageUrls.ReplaceRange(urls);
        }

        HasImages = ImageUrls.Count > 0;
        OnImagesChanged();
    }

    /// <summary>Ersetzt ein einzelnes Bild (z.B. Thumbnail -> geladene Vorschau).</summary>
    private void PatchImage(int index, string url)
    {
        if (index < 0 || index >= ImageUrls.Count)
            return;

        if (string.Equals(ImageUrls[index], url, StringComparison.Ordinal))
            return;

        ImageUrls[index] = url;

        if (index == CurrentImagePosition)
            OnPropertyChanged(nameof(CurrentImageUrl));
    }

    /// <summary>
    /// Kernfakten aus den Listendaten (ohne TypeSpecificData) - dieselben Kacheln wie
    /// nach dem Laden, nur aus den groberen Listenfeldern.
    /// </summary>
    private List<StatTileItem> BuildHandoffStatTiles(PropertyListItemDto item)
    {
        var tiles = new List<StatTileItem>();

        if (item.Type == PropertyType.Foreclosure)
        {
            if (item.PlotAreaM2 is > 0)
                tiles.Add(new StatTileItem(_loc.TileArea, PropertyDisplay.Area(item.PlotAreaM2.Value)));
            if (item.LivingAreaM2 is > 0)
                tiles.Add(new StatTileItem(_loc.TileBuilt, PropertyDisplay.Area(item.LivingAreaM2.Value)));
            if (item.AuctionDate is { } auctionDate)
                tiles.Add(new StatTileItem(_loc.TileAuctionDate, auctionDate.ToLocalTime().ToString("dd.MM.yy")));
        }
        else
        {
            if (item.LivingAreaM2 is > 0)
                tiles.Add(new StatTileItem(_loc.TileLivingArea, PropertyDisplay.Area(item.LivingAreaM2.Value)));
            if (item.PlotAreaM2 is > 0)
                tiles.Add(new StatTileItem(_loc.TilePlot, PropertyDisplay.Area(item.PlotAreaM2.Value)));
            if (item.Rooms is > 0)
                tiles.Add(new StatTileItem(_loc.TileRooms, item.Rooms.Value.ToString()));
        }

        return tiles;
    }

    private string GetTypeBadgeText(PropertyType type) => type switch
    {
        PropertyType.House => _loc.TypeBadgeHouse,
        PropertyType.Land => _loc.TypeBadgeLand,
        PropertyType.Foreclosure => _loc.TypeBadgeForeclosure,
        _ => _loc.TypeBadgeDefault
    };

    /// <summary>
    /// Typ-Farben (analog zur PropertyCard, wie Web): ZV = Signal-Rot,
    /// Grund = Gruen, Haus = Blau, Rest warmes Glas
    /// </summary>
    private static Color GetTypeBadgeColor(PropertyType type) => type switch
    {
        PropertyType.Foreclosure => Color.FromArgb("#DE2A2F"),
        PropertyType.Land => Color.FromArgb("#33854A"),
        PropertyType.House => Color.FromArgb("#2F6E9E"),
        _ => Color.FromArgb("#66171310")
    };

    /// <summary>
    /// Nutzt den beim Antippen der Karte gestarteten Request, falls vorhanden - der
    /// laeuft dann schon waehrend Seitenaufbau und Navigationsanimation. Sonst (Deep-Link,
    /// erneuter Ladeversuch, Delta-Sync) wird wie bisher selbst angefragt.
    /// </summary>
    private async Task<GetPropertyByIdResponse?> RequestPropertyAsync(Guid propertyId)
    {
        var pending = _detailPreloader.TryTakePendingRequest(propertyId);
        if (pending != null)
            return await pending;

        var (_, response) = await _mediator.Request(new GetPropertyByIdHttpRequest { Id = propertyId });
        return response;
    }

    private async Task LoadPropertyAsync(Guid propertyId)
    {
        HasLoadError = false;
        LoadErrorText = null;
        _onlineWaitCts?.Cancel();

        // Erst zeichnen, dann eintragen: Der vorgeladene Request ist beim Aufruf aus
        // OnAppearing oft schon fertig. Ohne diesen Bruch laeuft das komplette
        // Uebernehmen der Detaildaten (Kacheln, Sektionen, Kontakte) noch im
        // Tap-Handler - also mitten in der Navigationsanimation. Die Vorschau aus den
        // Listendaten steht zu diesem Zeitpunkt bereits, sichtbar fehlt also nichts.
        await Task.Yield();

        var busyCts = new CancellationTokenSource();
        _ = ShowBusyAfterDelayAsync(busyCts.Token);

        try
        {
            _logger.LogInformation("[PropertyDetail] Loading property {PropertyId} from API", propertyId);

            var response = await RequestPropertyAsync(propertyId);
            IsShowingCachedData = _offlineReadState.IsBackendUnavailable;

            if (response?.Property != null)
            {
                Property = response.Property;
                _logger.LogInformation("[PropertyDetail] Property loaded: {Title}", Property.Title);
            }
            else if (_internet.IsAvailable)
            {
                // Der Server kennt die Immobilie nicht mehr - wirklich geloescht/abgelaufen
                Property = null;
                _logger.LogWarning("[PropertyDetail] Property {PropertyId} not found", propertyId);
                SetLoadError(
                    "icon_unavailable.png",
                    _loc.NotAvailableTitle,
                    _loc.NotAvailableText,
                    canRetry: false);
            }
            else
            {
                Property = null;
                SetOfflineError(propertyId);
            }

            // Favoriten-Status laden
            var isFavorite = false;
            if (_authService.IsAuthenticated)
            {
                await _propertyStatusService.EnsureLoadedAsync();
                isFavorite = _propertyStatusService.IsFavorite(propertyId);
            }

            UpdateDisplayProperties();
            IsFavorite = isFavorite;
            _trace.Mark(propertyId, "Detailseite vollstaendig");
        }
        catch (Exception ex) when (ex is OfflineDataUnavailableException || !_internet.IsAvailable)
        {
            // Kein Internet und (noch) keine lokal gespeicherte Antwort - die Immobilie
            // existiert weiterhin, sie kann nur gerade nicht geladen werden
            _logger.LogInformation("[PropertyDetail] Offline ohne lokale Daten fuer {PropertyId}", propertyId);
            Property = null;
            IsShowingCachedData = _offlineReadState.IsBackendUnavailable;
            UpdateDisplayProperties();
            SetOfflineError(propertyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PropertyDetail] Error loading property {PropertyId}", propertyId);
            Property = null;
            IsShowingCachedData = _offlineReadState.IsBackendUnavailable;
            UpdateDisplayProperties();
            SetLoadError(
                "icon_offline.png",
                _loc.LoadFailedTitle,
                ex is HttpRequestException
                    ? _loc.ServerUnreachableText
                    : _loc.TryAgainLaterText,
                canRetry: true);
        }
        finally
        {
            busyCts.Cancel();
            busyCts.Dispose();
            IsBusy = false;
            BusyMessage = null;
        }
    }

    /// <summary>
    /// Wie lange gewartet wird, bevor das Lade-Overlay erscheint. Cache-Treffer und
    /// vorgeladene Requests sind deutlich schneller - ohne diese Verzoegerung wuerde
    /// bei jedem Oeffnen ein Spinner aufblitzen.
    /// </summary>
    private static readonly TimeSpan BusyOverlayDelay = TimeSpan.FromMilliseconds(250);

    /// <summary>
    /// Zeigt das Lade-Overlay nur, wenn nach <see cref="BusyOverlayDelay"/> noch immer
    /// nichts anzuzeigen ist. Steht bereits Inhalt auf der Seite (Vorschau aus den
    /// Listendaten oder ein frueheres Ergebnis), bleibt es ganz aus - ein Vollbild-Dimmer
    /// ueber sichtbarem Inhalt wirkt wie ein Ruckler.
    /// </summary>
    private async Task ShowBusyAfterDelayAsync(CancellationToken cancellationToken)
    {
        if (Property != null || _hasHandoffPreview)
            return;

        try
        {
            await Task.Delay(BusyOverlayDelay, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (cancellationToken.IsCancellationRequested)
            return;

        BusyMessage = _loc.BusyLoading;
        IsBusy = true;
    }

    private void SetOfflineError(Guid propertyId)
    {
        SetLoadError(
            "icon_offline.png",
            _loc.OfflineTitle,
            _loc.OfflineText,
            canRetry: true);
        StartAutoReloadWhenOnline(propertyId);
    }

    private void SetLoadError(string iconSource, string title, string text, bool canRetry)
    {
        LoadErrorIcon = iconSource;
        LoadErrorTitle = title;
        LoadErrorText = text;
        CanRetryLoad = canRetry;
        HasLoadError = true;
    }

    /// <summary>
    /// Laedt automatisch neu, sobald die Internetverbindung zurueckkehrt,
    /// solange der Offline-Zustand auf dieser Seite sichtbar ist.
    /// </summary>
    private void StartAutoReloadWhenOnline(Guid propertyId)
    {
        _onlineWaitCts?.Cancel();
        var cts = new CancellationTokenSource();
        _onlineWaitCts = cts;

        _ = Task.Run(async () =>
        {
            try
            {
                await _internet.WaitForAvailable(cts.Token).ConfigureAwait(false);
                if (!cts.IsCancellationRequested)
                    MainThread.BeginInvokeOnMainThread(() => _ = LoadPropertyAsync(propertyId));
            }
            catch (OperationCanceledException)
            {
                // Seite verlassen oder neuer Ladeversuch gestartet
            }
        });
    }

    [RelayCommand]
    private Task RetryLoadAsync()
        => Guid.TryParse(PropertyId, out var id) ? LoadPropertyAsync(id) : Task.CompletedTask;

    private void UpdateDisplayProperties()
    {
        if (Property == null)
        {
            // Mit dem Inhalt ist auch die Vorschau aus den Listendaten weg - ein
            // erneuter Ladeversuch muss daher wieder eine Ladeanzeige bekommen
            _hasHandoffPreview = false;

            Title = _loc.FallbackTitle;
            FormattedPrice = string.Empty;
            AddressText = string.Empty;
            ContactPersonText = string.Empty;
            HasContactPerson = false;
            IsBroker = false;
            OriginalListingUrl = null;
            HasOriginalListingUrl = false;
            _fullImageUrls = [];
            _previewImageUrls = [];
            SetImages([]);
            SellerName = null;
            Contacts = [];
            HasContacts = false;
            HasContactFooter = false;
            IsContactExpanded = false;
            CurrentImagePosition = 0;
            OnPropertyChanged(nameof(ImageCounterText));
            TypeBadgeText = string.Empty;
            DetailSections = [];
            StatTiles = [];
            HasStatTiles = false;
            Description = null;
            HasDescription = false;
            return;
        }

        Title = Property.Title;
        TypeBadgeText = GetTypeBadgeText(Property.Type);
        TypeBadgeColor = GetTypeBadgeColor(Property.Type);

        // Preis formatieren: "€ 3.590.000"
        FormattedPrice = PropertyDisplay.Price((decimal)Property.Price);

        // Volle Adresse: "Strasse, PLZ Ort"
        AddressText = $"{Property.Address}, {Property.PostalCode} {Property.City}";

        // Makler vs. Privat
        IsBroker = Property.SellerType == SellerType.Broker;
        OriginalListingUrl = Property.Contacts?.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.OriginalListingUrl))?.OriginalListingUrl;

        // Fuer Zwangsversteigerungen: EdictUrl aus TypeSpecificData extrahieren wenn keine Kontakt-URL
        if (string.IsNullOrWhiteSpace(OriginalListingUrl) && !string.IsNullOrWhiteSpace(Property.TypeSpecificData))
        {
            try
            {
                using var doc = JsonDocument.Parse(Property.TypeSpecificData);
                if (doc.RootElement.TryGetProperty("EdictUrl", out var edictUrlElement))
                    OriginalListingUrl = edictUrlElement.GetString();
            }
            catch
            {
                // Parse-Fehler ignorieren
            }
        }
        HasOriginalListingUrl = !string.IsNullOrWhiteSpace(OriginalListingUrl);

        // Bilder: angezeigt wird die Vorschau-Variante (bzw. solange das Thumbnail,
        // das die Karte schon geladen hat), die volle Aufloesung nur im Vollbild-Viewer
        ApplyImageVariants();

        // Kontaktperson (erster Kontakt falls vorhanden); ausblenden wenn identisch
        // mit dem Anbieternamen (sonst steht derselbe Name doppelt im Footer)
        // Nur der Name: "Herr/Frau"-Praefix waere bei Firmenkontakten (GmbH, Makler) falsch
        var firstContact = Property.Contacts?.FirstOrDefault();
        ContactPersonText = firstContact?.Name ?? string.Empty;
        HasContactPerson = !string.IsNullOrWhiteSpace(ContactPersonText)
            && !string.Equals(ContactPersonText, Property.SellerName, StringComparison.OrdinalIgnoreCase);

        // Alle Kontakte fuer den aufklappbaren Kontaktbereich; Kontakte ohne
        // E-Mail/Telefon (nur Original-URL) deckt der Originalinserat-Button ab.
        // Kontaktname = Anbietername wird unterdrueckt (steht schon in der Kopfzeile).
        SellerName = Property.SellerName;
        Contacts = Property.Contacts?
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new ContactDisplayItem(
                string.Equals(c.Name, Property.SellerName, StringComparison.OrdinalIgnoreCase)
                    ? string.Empty
                    : c.Name ?? string.Empty,
                c.Email,
                c.Phone))
            .Where(c => c.HasEmail || c.HasPhone)
            .ToList() ?? [];
        HasContacts = Contacts.Count > 0;
        HasContactFooter = HasContacts || !string.IsNullOrWhiteSpace(SellerName);

        // Beschreibung
        Description = Property.Description;
        HasDescription = !string.IsNullOrWhiteSpace(Property.Description);

        // Strukturierte Datentabelle aufbauen (im Hintergrund, s.u.)
        StartDetailContentBuild();
    }

    /// <summary>
    /// Baut Datentabelle und Kernfakten im Hintergrund und setzt sie danach auf dem
    /// UI-Thread. Kopf und Foto sind damit schon gezeichnet, bevor die Tabelle mit
    /// ihren rund drei Dutzend Zeilen ins Layout kommt.
    /// </summary>
    private void StartDetailContentBuild()
    {
        var property = Property;
        if (property == null)
            return;

        var formattedPrice = FormattedPrice;

        _ = Task.Run(() =>
        {
            var (sections, tiles) = BuildDetailContent(property, formattedPrice);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Zwischenzeitlicher Wechsel auf eine andere Immobilie (Delta-Sync,
                // Deep-Link) darf keine fremde Tabelle einspielen
                if (Property?.Id != property.Id)
                    return;

                DetailSections = sections;
                StatTiles = tiles;
                HasStatTiles = tiles.Count > 0;
            });
        });
    }

    /// <summary>
    /// Uebernimmt die drei Bild-Varianten der geladenen Immobilie: sofort sichtbar wird
    /// die beste lokal vorhandene (Vorschau, sonst Thumbnail), die volle Aufloesung
    /// bleibt dem Vollbild-Viewer vorbehalten. Fehlen die skalierten Listen (Antwort aus
    /// einem Offline-Cache von vor diesem Feld), dienen die vollen URLs als Fallback.
    /// </summary>
    private void ApplyImageVariants()
    {
        var full = Property?.ImageUrls?.Where(url => !string.IsNullOrEmpty(url)).ToList() ?? [];
        var previews = NonEmptyOrFallback(Property?.PreviewImageUrls, full);
        var thumbnails = NonEmptyOrFallback(Property?.ThumbnailImageUrls, previews);

        _fullImageUrls = full;
        _previewImageUrls = previews;

        SetImages(_imageResolver.ResolveDisplayUrls(previews, thumbnails));

        StartPreviewUpgrade();
    }

    private static List<string> NonEmptyOrFallback(IEnumerable<string>? candidate, List<string> fallback)
    {
        var list = candidate?.Where(url => !string.IsNullOrEmpty(url)).ToList();
        return list is { Count: > 0 } ? list : fallback;
    }

    /// <summary>
    /// Laedt die Vorschau-Varianten in den Bild-Cache und ersetzt die angezeigten
    /// Thumbnails, sobald die scharfe Variante lokal vorliegt.
    /// </summary>
    private void StartPreviewUpgrade()
    {
        _imageUpgradeCts?.Cancel();
        _imageUpgradeCts?.Dispose();
        _imageUpgradeCts = null;

        if (_previewImageUrls.Count == 0)
            return;

        var cts = new CancellationTokenSource();
        _imageUpgradeCts = cts;

        var propertyId = Property?.Id;
        var previews = _previewImageUrls;

        _ = Task.Run(() => _imageResolver.UpgradeToPreviewsAsync(
            previews,
            CurrentImagePosition,
            (index, resolved) => MainThread.BeginInvokeOnMainThread(() =>
            {
                // Zwischenzeitlicher Wechsel auf eine andere Immobilie (Delta-Sync,
                // Deep-Link) darf keine fremden Bilder einspielen
                if (cts.IsCancellationRequested || Property?.Id != propertyId)
                    return;

                PatchImage(index, resolved);

                if (index == 0 && propertyId is { } id)
                    _trace.Complete(id, "Hero-Foto scharf");
            }),
            cts.Token));
    }

    /// <summary>
    /// Baut Datentabelle und Kernfakten-Kacheln aus der geladenen Immobilie. Greift
    /// bewusst auf keinen View-State zu, damit der Aufbau (JSON-Parsen plus rund drei
    /// Dutzend Eintraege) im Hintergrund laufen kann und den ersten Frame der Seite
    /// nicht aufhaelt; gesetzt wird das Ergebnis auf dem UI-Thread.
    /// </summary>
    private (List<PropertyDetailSection> Sections, List<StatTileItem> Tiles) BuildDetailContent(
        PropertyDto property,
        string formattedPrice)
    {
        var items = new List<PropertyDetailItem>();
        var price = (decimal)property.Price;
        var isForeclosure = property.Type == PropertyType.Foreclosure;

        // TypeSpecificData deserialisieren (vor den Basisdaten: das Preis-Label haengt bei
        // Zwangsversteigerungen davon ab, ob Price aus MinimumBid oder EstimatedValue stammt)
        HousePropertyData? houseData = null;
        LandPropertyData? landData = null;
        ForeclosurePropertyData? foreclosureData = null;

        if (!string.IsNullOrWhiteSpace(property.TypeSpecificData))
        {
            try
            {
                switch (property.Type)
                {
                    case PropertyType.House:
                        houseData = JsonSerializer.Deserialize<HousePropertyData>(property.TypeSpecificData, JsonOptions);
                        break;
                    case PropertyType.Land:
                        landData = JsonSerializer.Deserialize<LandPropertyData>(property.TypeSpecificData, JsonOptions);
                        break;
                    case PropertyType.Foreclosure:
                        foreclosureData = JsonSerializer.Deserialize<ForeclosurePropertyData>(property.TypeSpecificData, JsonOptions);
                        break;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "[PropertyDetail] Failed to deserialize TypeSpecificData");
            }
        }

        // --- Basisdaten (hervorgehoben) ---
        var typeLabel = property.Type switch
        {
            PropertyType.House => _loc.TypeHouse,
            PropertyType.Land => _loc.TypeLand,
            PropertyType.Foreclosure => _loc.TypeForeclosure,
            _ => property.Type.ToString()
        };
        items.Add(new PropertyDetailItem(_loc.LabelPropertyType, typeLabel, PropertyDataCategory.Basisdaten, true));

        // Preis-Label kommt serverseitig berechnet (Backend-First: "Kaufpreis" bzw. bei
        // Zwangsversteigerungen "Schätzwert"/"Mindestgebot"). Fallback nur fuer Detail-
        // Antworten aus dem Offline-Cache von vor diesem Feld (Record-Default "Kaufpreis").
        // Bewusst NICHT lokalisiert: die Literale spiegeln serverseitige Anzeige-Texte 1:1
        // und der Vergleich unten prueft gegen genau diesen Server-Text.
        var priceLabel = string.IsNullOrWhiteSpace(property.PriceLabel) ? "Kaufpreis" : property.PriceLabel;
        if (isForeclosure && priceLabel == "Kaufpreis")
        {
            priceLabel = foreclosureData is { MinimumBid: <= 0, EstimatedValue: > 0 } ? "Schätzwert" : "Mindestgebot";
        }
        items.Add(new PropertyDetailItem(priceLabel, price > 0 ? formattedPrice : _loc.PriceOnRequest, PropertyDataCategory.Basisdaten, true));
        AddIfNotEmpty(items, _loc.LabelPostalCode, property.PostalCode, PropertyDataCategory.Basisdaten, true);
        AddIfNotEmpty(items, _loc.LabelCity, property.City, PropertyDataCategory.Basisdaten, true);
        AddIfNotEmpty(items, _loc.LabelAddress, property.Address, PropertyDataCategory.Basisdaten, true);

        // --- Flaechen ---
        // Bei Zwangsversteigerungen tragen die Kernfelder Edikt-Semantik (LivingAreaM2 = bebaute
        // Flaeche, PlotAreaM2 = Gesamtflaeche) - unter Haus-Labels waeren die Werte falsch.
        if (isForeclosure)
        {
            if (foreclosureData?.TotalArea is > 0)
                items.Add(new PropertyDetailItem(_loc.LabelTotalArea, PropertyDisplay.Area(foreclosureData.TotalArea.Value), PropertyDataCategory.Flaechen));
            else
                AddIfHasValue(items, _loc.LabelPlotArea, property.PlotAreaM2, v => PropertyDisplay.Area(v), PropertyDataCategory.Flaechen);

            if (foreclosureData?.BuildingArea is > 0)
                items.Add(new PropertyDetailItem(_loc.LabelBuildingArea, PropertyDisplay.Area(foreclosureData.BuildingArea.Value), PropertyDataCategory.Flaechen));
            else
                AddIfHasValue(items, _loc.LabelBuildingArea, property.LivingAreaM2, v => PropertyDisplay.Area(v), PropertyDataCategory.Flaechen);
        }
        else
        {
            if (houseData != null && houseData.LivingAreaInSquareMeters > 0)
                items.Add(new PropertyDetailItem(_loc.LabelLivingArea, PropertyDisplay.Area(houseData.LivingAreaInSquareMeters), PropertyDataCategory.Flaechen));
            else
                AddIfHasValue(items, _loc.LabelLivingArea, property.LivingAreaM2, v => PropertyDisplay.Area(v), PropertyDataCategory.Flaechen);

            if (landData != null && landData.PlotSizeInSquareMeters > 0)
                items.Add(new PropertyDetailItem(_loc.LabelPlotArea, PropertyDisplay.Area(landData.PlotSizeInSquareMeters), PropertyDataCategory.Flaechen));
            else
                AddIfHasValue(items, _loc.LabelPlotArea, property.PlotAreaM2, v => PropertyDisplay.Area(v), PropertyDataCategory.Flaechen);
        }

        // --- Gebaeude (Haus) ---
        if (houseData != null)
        {
            if (houseData.TotalRooms > 0)
                items.Add(new PropertyDetailItem(_loc.LabelRooms, houseData.TotalRooms.ToString(), PropertyDataCategory.Gebaeude));
            else
                AddIfHasValue(items, _loc.LabelRooms, property.Rooms, v => v.ToString(), PropertyDataCategory.Gebaeude);

            if (houseData.Bedrooms > 0)
                items.Add(new PropertyDetailItem(_loc.LabelBedrooms, houseData.Bedrooms.ToString(), PropertyDataCategory.Gebaeude));
            if (houseData.Bathrooms > 0)
                items.Add(new PropertyDetailItem(_loc.LabelBathrooms, houseData.Bathrooms.ToString(), PropertyDataCategory.Gebaeude));
            if (houseData.Floors > 0)
                items.Add(new PropertyDetailItem(_loc.LabelFloors, houseData.Floors.ToString(), PropertyDataCategory.Gebaeude));
            if (houseData.YearBuilt.HasValue)
                items.Add(new PropertyDetailItem(_loc.LabelYearBuilt, houseData.YearBuilt.Value.ToString(), PropertyDataCategory.Gebaeude));
            else
                AddIfHasValue(items, _loc.LabelYearBuilt, property.YearBuilt, v => v.ToString(), PropertyDataCategory.Gebaeude);

            // Default(0) ist kein definierter Enum-Wert (z.B. bei leerem TypeSpecificData "{}")
            if (Enum.IsDefined(houseData.Condition))
                items.Add(new PropertyDetailItem(_loc.LabelCondition, FormatCondition(houseData.Condition), PropertyDataCategory.Gebaeude));

            if (houseData.ApartmentFloor.HasValue)
                items.Add(new PropertyDetailItem(_loc.LabelApartmentFloor, houseData.ApartmentFloor.Value.ToString(), PropertyDataCategory.Gebaeude));
        }
        else
        {
            // Fallback auf Kernfelder ohne HousePropertyData
            AddIfHasValue(items, _loc.LabelRooms, property.Rooms, v => v.ToString(), PropertyDataCategory.Gebaeude);
            AddIfHasValue(items, _loc.LabelYearBuilt, property.YearBuilt, v => v.ToString(), PropertyDataCategory.Gebaeude);
        }

        // --- Ausstattung (Haus) ---
        if (houseData != null)
        {
            if (houseData.HasGarage)
                items.Add(new PropertyDetailItem(_loc.LabelGarage, _commonLoc.Yes, PropertyDataCategory.Ausstattung));
            if (houseData.HasGarden)
                items.Add(new PropertyDetailItem(_loc.LabelGarden, _commonLoc.Yes, PropertyDataCategory.Ausstattung));
            if (houseData.HasBasement)
                items.Add(new PropertyDetailItem(_loc.LabelBasement, _commonLoc.Yes, PropertyDataCategory.Ausstattung));
            if (houseData.HasElevator == true)
                items.Add(new PropertyDetailItem(_loc.LabelElevator, _commonLoc.Yes, PropertyDataCategory.Ausstattung));
        }

        // --- Grundstueck (Land) ---
        if (landData != null)
        {
            if (Enum.IsDefined(landData.Zoning))
                items.Add(new PropertyDetailItem(_loc.LabelZoning, FormatZoning(landData.Zoning), PropertyDataCategory.Grundstueck));
            items.Add(new PropertyDetailItem(_loc.LabelBuildingRights, FormatBool(landData.HasBuildingRights), PropertyDataCategory.Grundstueck));
            items.Add(new PropertyDetailItem(_loc.LabelBuildable, FormatBool(landData.IsBuildable), PropertyDataCategory.Grundstueck));
            items.Add(new PropertyDetailItem(_loc.LabelUtilities, FormatBool(landData.HasUtilities), PropertyDataCategory.Grundstueck));
            if (landData.SoilQuality.HasValue)
                items.Add(new PropertyDetailItem(_loc.LabelSoilQuality, FormatSoilQuality(landData.SoilQuality.Value), PropertyDataCategory.Grundstueck));
        }

        // --- Versteigerung (Foreclosure) ---
        if (foreclosureData != null)
        {
            AddIfNotEmpty(items, _loc.LabelCourt, foreclosureData.CourtName, PropertyDataCategory.Versteigerung);
            AddIfNotEmpty(items, _loc.LabelFileNumber, foreclosureData.FileNumber, PropertyDataCategory.Versteigerung);
            if (foreclosureData.AuctionDate != default)
                items.Add(new PropertyDetailItem(_loc.LabelAuctionDate, foreclosureData.AuctionDate.ToLocalTime().ToString("dd.MM.yyyy"), PropertyDataCategory.Versteigerung));
            if (foreclosureData.MinimumBid > 0)
                items.Add(new PropertyDetailItem(_loc.LabelMinimumBid, PropertyDisplay.Price(foreclosureData.MinimumBid), PropertyDataCategory.Versteigerung));
            if (foreclosureData.EstimatedValue is > 0)
                items.Add(new PropertyDetailItem(_loc.LabelEstimatedValue, PropertyDisplay.Price(foreclosureData.EstimatedValue.Value), PropertyDataCategory.Versteigerung));
            if (Enum.IsDefined(foreclosureData.Status))
                items.Add(new PropertyDetailItem(_loc.LabelStatus, PropertyDisplay.LegalStatusText(foreclosureData.Status), PropertyDataCategory.Versteigerung));
        }

        // --- Kosten ---
        // Preis/m² kommt serverseitig berechnet (null bei Zwangsversteigerungen - dort waere
        // es Mindestgebot / bebaute Flaeche). Fallback nur fuer Offline-Cache-Antworten
        // von vor diesem Feld.
        decimal? pricePerSqm = property.PricePerSquareMeter is > 0
            ? (decimal)property.PricePerSquareMeter.Value
            : !isForeclosure && price > 0 && property.LivingAreaM2 is > 0
                ? price / property.LivingAreaM2.Value
                : null;
        if (pricePerSqm is > 0)
            // Gerundet wie am Web ("€ 3.714") - Cent-Betraege sagen hier nichts aus
            items.Add(new PropertyDetailItem(_loc.LabelPricePerSqm, PropertyDisplay.Price(pricePerSqm.Value), PropertyDataCategory.Kosten));

        // --- Basisdaten: Eingestellt am ---
        items.Add(new PropertyDetailItem(_loc.LabelCreatedAt, property.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy"), PropertyDataCategory.Basisdaten));

        // Nach Kategorie gruppieren, leere Sektionen entfallen automatisch
        var sections = items
            .GroupBy(i => i.Category)
            .OrderBy(g => g.Key)
            .Select(g => new PropertyDetailSection(GetCategoryTitle(g.Key), g.Key, g.ToList()))
            .ToList();

        return (sections, BuildStatTiles(property, houseData, landData, foreclosureData));
    }

    /// <summary>
    /// Kernfakten fuer die Kachel-Zeile unter dem Kopf. Gleiche Wertequellen wie die
    /// Datentabelle (TypeSpecificData vor Kernfeldern), aber auf drei Werte verdichtet.
    /// </summary>
    private List<StatTileItem> BuildStatTiles(
        PropertyDto property,
        HousePropertyData? houseData,
        LandPropertyData? landData,
        ForeclosurePropertyData? foreclosureData)
    {
        var tiles = new List<StatTileItem>();

        if (property.Type == PropertyType.Foreclosure)
        {
            decimal? totalArea = foreclosureData is { TotalArea: > 0 } fa ? fa.TotalArea : property.PlotAreaM2;
            if (totalArea is > 0)
                tiles.Add(new StatTileItem(_loc.TileArea, PropertyDisplay.Area(totalArea.Value)));

            decimal? buildingArea = foreclosureData is { BuildingArea: > 0 } fb ? fb.BuildingArea : property.LivingAreaM2;
            if (buildingArea is > 0)
                tiles.Add(new StatTileItem(_loc.TileBuilt, PropertyDisplay.Area(buildingArea.Value)));

            if (foreclosureData?.AuctionDate is { } auctionDate && auctionDate != default)
                tiles.Add(new StatTileItem(_loc.TileAuctionDate, auctionDate.ToLocalTime().ToString("dd.MM.yy")));
        }
        else
        {
            decimal? livingArea = houseData is { LivingAreaInSquareMeters: > 0 } hl
                ? hl.LivingAreaInSquareMeters
                : property.LivingAreaM2;
            if (livingArea is > 0)
                tiles.Add(new StatTileItem(_loc.TileLivingArea, PropertyDisplay.Area(livingArea.Value)));

            decimal? plotArea = landData is { PlotSizeInSquareMeters: > 0 } lp
                ? lp.PlotSizeInSquareMeters
                : property.PlotAreaM2;
            if (plotArea is > 0)
                tiles.Add(new StatTileItem(_loc.TilePlot, PropertyDisplay.Area(plotArea.Value)));

            int? rooms = houseData is { TotalRooms: > 0 } hr ? hr.TotalRooms : property.Rooms;
            if (rooms is > 0)
                tiles.Add(new StatTileItem(_loc.TileRooms, rooms.Value.ToString()));
        }

        return tiles;
    }

    #region Formatting Helpers

    private string FormatBool(bool value) => value ? _commonLoc.Yes : _commonLoc.No;

    private string FormatCondition(PropertyCondition condition) => condition switch
    {
        PropertyCondition.LikeNew => _loc.ConditionLikeNew,
        PropertyCondition.Good => _loc.ConditionGood,
        PropertyCondition.Average => _loc.ConditionAverage,
        PropertyCondition.NeedsRenovation => _loc.ConditionNeedsRenovation,
        _ => condition.ToString()
    };

    private string FormatZoning(ZoningType zoning) => zoning switch
    {
        ZoningType.Residential => _loc.ZoningResidential,
        ZoningType.Commercial => _loc.ZoningCommercial,
        ZoningType.Industrial => _loc.ZoningIndustrial,
        ZoningType.Agricultural => _loc.ZoningAgricultural,
        ZoningType.Mixed => _loc.ZoningMixed,
        _ => zoning.ToString()
    };

    private string FormatSoilQuality(SoilQuality quality) => quality switch
    {
        SoilQuality.High => _loc.SoilQualityHigh,
        SoilQuality.Medium => _loc.SoilQualityMedium,
        SoilQuality.Low => _loc.SoilQualityLow,
        _ => quality.ToString()
    };

    private string GetCategoryTitle(PropertyDataCategory category) => category switch
    {
        PropertyDataCategory.Basisdaten => _loc.SectionBasicData,
        PropertyDataCategory.Flaechen => _loc.SectionAreas,
        PropertyDataCategory.Gebaeude => _loc.SectionBuilding,
        PropertyDataCategory.Ausstattung => _loc.SectionEquipment,
        PropertyDataCategory.Grundstueck => _loc.SectionPlot,
        PropertyDataCategory.Versteigerung => _loc.SectionAuction,
        PropertyDataCategory.Rechtliches => _loc.SectionLegal,
        PropertyDataCategory.Grundbuch => _loc.SectionLandRegistry,
        PropertyDataCategory.Termine => _loc.SectionDates,
        PropertyDataCategory.Kosten => _loc.SectionCosts,
        PropertyDataCategory.Sonstiges => _loc.SectionMisc,
        _ => category.ToString().ToUpperInvariant()
    };

    private static void AddIfNotEmpty(List<PropertyDetailItem> items, string label, string? value, PropertyDataCategory category, bool highlighted = false)
    {
        if (!string.IsNullOrWhiteSpace(value))
            items.Add(new PropertyDetailItem(label, value, category, highlighted));
    }

    private static void AddIfHasValue<T>(List<PropertyDetailItem> items, string label, T? value, Func<T, string> format, PropertyDataCategory category) where T : struct
    {
        if (value.HasValue)
            items.Add(new PropertyDetailItem(label, format(value.Value), category));
    }

    #endregion

    #region Commands

    /// <summary>
    /// Kopiert einen Text in die Zwischenablage
    /// </summary>
    [RelayCommand]
    private async Task CopyToClipboardAsync(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        var success = await _clipboardService.CopyToClipboardAsync(text);
        if (success)
        {
            await ShowCopyFeedbackAsync(_loc.CopiedFeedback, 1500);
        }
    }

    /// <summary>
    /// Wechselt den Favoriten-Status der aktuellen Immobilie
    /// </summary>
    [RelayCommand]
    private async Task ToggleFavoriteAsync()
    {
        if (Property == null || !_authService.IsAuthenticated)
            return;

        _logger.LogInformation("[PropertyDetail] Toggling favorite for {PropertyId}", Property.Id);

        IsFavorite = await _propertyStatusService.ToggleFavoriteAsync(Property.Id);
    }

    /// <summary>
    /// Teilt die Immobilie ueber nativen Share-Dialog oder Zwischenablage
    /// </summary>
    [RelayCommand]
    private async Task SharePropertyAsync()
    {
        if (Property == null)
            return;

        _logger.LogInformation("[PropertyDetail] Sharing property {PropertyId}", Property.Id);

        // Web-Pendant des aktiven Endpunkts: Debug/Test-Builds teilen Test-Links
        var propertyUrl = Services.WebLinks.ListingUrl(_apiEndpoints.CurrentUrl, Property.Id);

        var description = $"{Property.Title}\n" +
                          $"{FormattedPrice}\n" +
                          $"{AddressText}";

        var result = await _shareService.ShareLinkAsync(Property.Title, propertyUrl, description);
        if (result == ShareResult.SharedNatively)
        {
            await ShowCopyFeedbackAsync(_loc.SharedFeedback, 2000);
        }
        else if (result == ShareResult.CopiedToClipboard)
        {
            await ShowCopyFeedbackAsync(_loc.CopiedToClipboardFeedback, 2000);
        }
    }

    /// <summary>
    /// Klappt die Kontaktdetails im Footer auf/zu
    /// </summary>
    [RelayCommand]
    private void OpenImageViewer()
    {
        if (HasImages)
            IsImageViewerOpen = true;
    }

    [RelayCommand]
    private void CloseImageViewer()
        => IsImageViewerOpen = false;

    [RelayCommand]
    private void ShowPreviousImage()
    {
        if (ImageUrls.Count == 0)
            return;
        CurrentImagePosition = CurrentImagePosition <= 0 ? ImageUrls.Count - 1 : CurrentImagePosition - 1;
    }

    [RelayCommand]
    private void ShowNextImage()
    {
        if (ImageUrls.Count == 0)
            return;
        CurrentImagePosition = CurrentImagePosition >= ImageUrls.Count - 1 ? 0 : CurrentImagePosition + 1;
    }

    [RelayCommand]
    private void ToggleContactExpanded()
    {
        IsContactExpanded = !IsContactExpanded;
        _logger.LogDebug("[PropertyDetail] ToggleContactExpanded -> {State}", IsContactExpanded);
    }

    /// <summary>
    /// Oeffnet die Mail-App fuer eine Kontakt-E-Mail, Fallback: Adresse kopieren
    /// </summary>
    [RelayCommand]
    private async Task ContactByEmailAsync(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return;

        try
        {
            var subject = Uri.EscapeDataString(_loc.EmailSubjectFormat(Property?.Title));
            await Launcher.Default.OpenAsync(new Uri($"mailto:{email}?subject={subject}"));
            return;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[PropertyDetail] Mail-App konnte nicht geoeffnet werden");
        }

        await CopyToClipboardAsync(email);
    }

    /// <summary>
    /// Oeffnet die Telefon-App fuer eine Kontakt-Nummer, Fallback: Nummer kopieren
    /// </summary>
    [RelayCommand]
    private async Task CallPhoneAsync(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return;

        try
        {
            await Launcher.Default.OpenAsync(new Uri($"tel:{phone.Replace(" ", string.Empty)}"));
            return;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[PropertyDetail] Telefon-App konnte nicht geoeffnet werden");
        }

        await CopyToClipboardAsync(phone);
    }

    /// <summary>
    /// Oeffnet die Original-Anbieterseite im Browser
    /// </summary>
    [RelayCommand]
    private async Task OpenOriginalListingAsync()
    {
        if (string.IsNullOrWhiteSpace(OriginalListingUrl))
            return;

        _logger.LogInformation("[PropertyDetail] Opening original listing: {Url}", OriginalListingUrl);

        await Launcher.Default.OpenAsync(new Uri(OriginalListingUrl));
    }

    private async Task ShowCopyFeedbackAsync(string message, int durationMs)
    {
        CopyFeedback = message;
        HasCopyFeedback = true;
        await Task.Delay(durationMs);
        CopyFeedback = null;
        HasCopyFeedback = false;
    }

    #endregion
}
