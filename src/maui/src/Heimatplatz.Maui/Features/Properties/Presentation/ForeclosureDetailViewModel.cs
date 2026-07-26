using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Core.Collections;
using Heimatplatz.Maui.Core.Media;
using Heimatplatz.Maui.Features.Auth;
using Heimatplatz.Maui.Features.Properties.Models;
using Heimatplatz.Maui.Features.Properties.Services;
using Heimatplatz.Maui.Localization.Properties;
using Heimatplatz.Maui.Offline;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;
using Shiny.Mediator.Infrastructure;

namespace Heimatplatz.Maui.Features.Properties.Presentation;

/// <summary>
/// ViewModel fuer die ForeclosureDetailPage (Zwangsversteigerung).
/// Laedt die Immobilie anhand der PropertyId via API (GetPropertyByIdHttpRequest).
/// </summary>
[ShellMap<ForeclosureDetailPage>("ForeclosureDetail")]
public partial class ForeclosureDetailViewModel : ObservableObject, IPageLifecycleAware
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
    private readonly ILogger<ForeclosureDetailViewModel> _logger;
    private readonly ForeclosureDetailStringsLocalized _loc;

    /// <summary>Lokalisierte Texte fuer XAML-Bindings (Loc.Key)</summary>
    public ForeclosureDetailStringsLocalized Loc => _loc;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Navigationsparameter: Id der zu ladenden Zwangsversteigerung
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

    /// <summary>
    /// Zwangsversteigerungen haben oft keinen Kaufpreis (0) - massgeblich sind
    /// Schaetzwert/Mindestgebot. Ein prominentes "0 €" wird dann ausgeblendet.
    /// </summary>
    [ObservableProperty]
    public partial bool HasPrice { get; set; }

    /// <summary>
    /// Beschriftung des prominenten Preises: Property.Price wird beim Sync aus
    /// MinimumBid ?? EstimatedValue befuellt - ein Kaufpreis existiert bei ZV nicht.
    /// </summary>
    [ObservableProperty]
    public partial string PriceCaption { get; set; }

    /// <summary>True wenn die Zwangsversteigerung nicht geladen werden konnte (Fehler oder geloescht)</summary>
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
    public partial string AddressText { get; set; }

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

    // === Dokument-URLs ===

    [ObservableProperty]
    public partial string? EdictUrl { get; set; }

    [ObservableProperty]
    public partial bool HasEdictUrl { get; set; }

    [ObservableProperty]
    public partial string? FloorPlanUrl { get; set; }

    [ObservableProperty]
    public partial bool HasFloorPlanUrl { get; set; }

    [ObservableProperty]
    public partial string? SitePlanUrl { get; set; }

    [ObservableProperty]
    public partial bool HasSitePlanUrl { get; set; }

    [ObservableProperty]
    public partial string? LongAppraisalUrl { get; set; }

    [ObservableProperty]
    public partial bool HasLongAppraisalUrl { get; set; }

    [ObservableProperty]
    public partial string? ShortAppraisalUrl { get; set; }

    [ObservableProperty]
    public partial bool HasShortAppraisalUrl { get; set; }

    [ObservableProperty]
    public partial bool HasDocuments { get; set; }

    [ObservableProperty]
    public partial bool HasImages { get; set; }

    /// <summary>
    /// Angezeigte Bilder (Thumbnail bis die Vorschau lokal vorliegt). Feste Instanz mit
    /// In-Place-Updates: ein Austausch der Liste wuerde die CarouselView komplett neu
    /// aufbauen, jedes Foto neu laden lassen und die Position zuruecksetzen. Die Items
    /// sind bewusst <see cref="DetailImage"/>-Objekte statt Strings: das Qualitaets-
    /// Upgrade wechselt nur deren Url-Binding und loest damit keine Collection-
    /// Notification aus (die wuerde auf Android jeden laufenden Swipe abbrechen).
    /// </summary>
    public ObservableRangeCollection<DetailImage> Images { get; } = [];

    /// <summary>Volle Display-Varianten - ausschliesslich fuer den Vollbild-Viewer</summary>
    private List<string> _fullImageUrls = [];

    /// <summary>Vorschau-Varianten (1280px), die im Hintergrund nachgeladen werden</summary>
    private List<string> _previewImageUrls = [];

    private CancellationTokenSource? _imageUpgradeCts;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ImageCounterText))]
    [NotifyPropertyChangedFor(nameof(CurrentImageUrl))]
    [NotifyPropertyChangedFor(nameof(CurrentFullImageUrl))]
    public partial int CurrentImagePosition { get; set; }

    [ObservableProperty]
    public partial string CourtName { get; set; }

    [ObservableProperty]
    public partial bool HasCourtName { get; set; }

    /// <summary>
    /// Bild-Zaehler fuer das Carousel, z.B. "2 / 7"
    /// </summary>
    public string ImageCounterText => Images.Count > 0
        ? $"{CurrentImagePosition + 1} / {Images.Count}"
        : string.Empty;

    /// <summary>
    /// Aktuelles Bild fuer den Windows-Bildviewer (Einzelbild statt CarouselView)
    /// </summary>
    public string? CurrentImageUrl => CurrentImagePosition >= 0 && CurrentImagePosition < Images.Count
        ? Images[CurrentImagePosition].Url
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
    public bool HasMultipleImages => Images.Count > 1;

    private void OnImagesChanged()
    {
        OnPropertyChanged(nameof(ImageCounterText));
        OnPropertyChanged(nameof(CurrentImageUrl));
        OnPropertyChanged(nameof(CurrentFullImageUrl));
        OnPropertyChanged(nameof(HasMultipleImages));
        OnPropertyChanged(nameof(ShowViewerNavigation));
    }

    /// <summary>
    /// Setzt die angezeigten Bilder. Bleibt die Anzahl gleich (Vorschau aus den
    /// Listendaten -> Detaildaten derselben Immobilie), werden nur die geaenderten
    /// Positionen ersetzt - die CarouselView behaelt dadurch ihre Items und ihre
    /// Position, statt alles neu zu laden.
    /// </summary>
    private void SetImages(IReadOnlyList<string> urls)
    {
        if (Images.Count == urls.Count)
        {
            for (var i = 0; i < urls.Count; i++)
                PatchImage(i, urls[i]);
        }
        else
        {
            // ReplaceRange (eine Reset-Notification) statt Clear + N Adds - siehe
            // PropertyDetailViewModel.SetImages: die Einzel-Notifications beenden die
            // App auf iOS mit "Invalid update: invalid number of items in section 0".
            Images.ReplaceRange(urls.Select(url => new DetailImage(url)));
        }

        HasImages = Images.Count > 0;
        OnImagesChanged();
    }

    /// <summary>
    /// Ersetzt ein einzelnes Bild (z.B. Thumbnail -> geladene Vorschau). Nur das
    /// Url-Binding des Items wechselt - keine Collection-Notification, siehe <see cref="Images"/>.
    /// </summary>
    private void PatchImage(int index, string url)
    {
        if (index < 0 || index >= Images.Count)
            return;

        if (string.Equals(Images[index].Url, url, StringComparison.Ordinal))
            return;

        Images[index].Url = url;

        if (index == CurrentImagePosition)
            OnPropertyChanged(nameof(CurrentImageUrl));
    }

    /// <summary>True solange der Vollbild-Bildviewer (Lightbox) offen ist</summary>
    [ObservableProperty]
    public partial bool IsImageViewerOpen { get; set; }

    /// <summary>Gericht-Footer ausblenden solange die Lightbox offen ist (liegt auf WinUI sonst ueber dem Backdrop)</summary>
    public bool ShowCourtFooter => HasCourtName && !IsImageViewerOpen;

    /// <summary>Pfeile im Vollbild-Viewer: nur offen UND mehrere Bilder (kombiniert, da WinUI verschachtelte IsVisible-Bindings nicht malt)</summary>
    public bool ShowViewerNavigation => IsImageViewerOpen && HasMultipleImages;

    partial void OnIsImageViewerOpenChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowCourtFooter));
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
                _logger.LogDebug(ex, "[ForeclosureDetail] Vollbild konnte nicht geladen werden");
                return;
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (!cts.IsCancellationRequested && CurrentImagePosition == index)
                    OnPropertyChanged(nameof(CurrentFullImageUrl));
            });
        });
    }

    partial void OnHasCourtNameChanged(bool value)
        => OnPropertyChanged(nameof(ShowCourtFooter));

    /// <summary>
    /// Text fuer den Favoriten-Button je nach Status
    /// </summary>
    public string FavoriteButtonText => IsFavorite ? _loc.FavoriteSaved : _loc.FavoriteSave;

    /// <summary>
    /// Icon fuer den Favoriten-Button (gefuelltes/leeres Herz)
    /// </summary>
    public string FavoriteButtonIcon => IsFavorite ? "♥" : "♡";

    public ForeclosureDetailViewModel(
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
        ILogger<ForeclosureDetailViewModel> logger,
        ForeclosureDetailStringsLocalized loc)
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

        Title = loc.PageTitle;
        LoadErrorIcon = string.Empty;
        LoadErrorTitle = string.Empty;
        FormattedPrice = string.Empty;
        PriceCaption = loc.PriceCaptionMinimumBid;
        AddressText = string.Empty;
        TypeBadgeText = loc.TypeBadge;
        TypeBadgeColor = Color.FromArgb("#DE2A2F");
        DetailSections = [];
        StatTiles = [];
        CourtName = string.Empty;
        IsAuthenticated = authService.IsAuthenticated;
    }

    #region IPageLifecycleAware

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
        }
        else
        {
            _logger.LogWarning("[ForeclosureDetail] Invalid PropertyId: {PropertyId}", PropertyId);
        }
    }

    public void OnDisappearing()
    {
        _offlineReadState.Changed -= OnOfflineReadStateChanged;
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
    /// ersetzt, sobald es vorliegt. Gericht und Versteigerungsdaten stecken in
    /// TypeSpecificData und kommen daher erst mit den Detaildaten.
    /// </summary>
    private void ApplyHandoff(Guid propertyId)
    {
        var item = _handoffCache.Get(propertyId);
        if (item == null)
            return;

        _hasHandoffPreview = true;

        Title = item.Title;
        HasPrice = item.Price > 0;
        FormattedPrice = HasPrice ? PropertyDisplay.Price((decimal)item.Price) : string.Empty;
        AddressText = $"{item.Address}, {item.PostalCode} {item.City}";

        var tiles = new List<StatTileItem>();
        if (item.AuctionDate is { } auctionDate)
            tiles.Add(new StatTileItem(_loc.TileAuctionDate, auctionDate.ToLocalTime().ToString("dd.MM.yyyy")));
        if (item.PlotAreaM2 is > 0)
            tiles.Add(new StatTileItem(_loc.TileArea, PropertyDisplay.Area(item.PlotAreaM2.Value)));
        StatTiles = tiles;
        HasStatTiles = tiles.Count > 0;

        // Die Listen-URLs sind die Thumbnails, die die Karte bereits geladen hat -
        // GetCachedOrOriginal loest sie auf den lokalen Dateipfad auf (kein Download)
        CurrentImagePosition = 0;
        SetImages(item.ImageUrls?
            .Where(url => !string.IsNullOrEmpty(url))
            .Select(_imageCache.GetCachedOrOriginal)
            .ToList() ?? []);
    }

    /// <summary>
    /// Nutzt den beim Antippen der Karte gestarteten Request, falls vorhanden - der
    /// laeuft dann schon waehrend Seitenaufbau und Navigationsanimation. Sonst (Deep-Link,
    /// erneuter Ladeversuch) wird wie bisher selbst angefragt.
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
            _logger.LogInformation("[ForeclosureDetail] Loading property {PropertyId} from API", propertyId);

            var response = await RequestPropertyAsync(propertyId);
            IsShowingCachedData = _offlineReadState.IsBackendUnavailable;

            if (response?.Property != null)
            {
                Property = response.Property;
                _logger.LogInformation("[ForeclosureDetail] Property loaded: {Title}", Property.Title);
            }
            else if (_internet.IsAvailable)
            {
                // Der Server kennt die Versteigerung nicht mehr - wirklich geloescht/abgelaufen
                Property = null;
                _logger.LogWarning("[ForeclosureDetail] Property {PropertyId} not found", propertyId);
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
            // Kein Internet und (noch) keine lokal gespeicherte Antwort - die Versteigerung
            // existiert weiterhin, sie kann nur gerade nicht geladen werden
            _logger.LogInformation("[ForeclosureDetail] Offline ohne lokale Daten fuer {PropertyId}", propertyId);
            Property = null;
            IsShowingCachedData = _offlineReadState.IsBackendUnavailable;
            UpdateDisplayProperties();
            SetOfflineError(propertyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ForeclosureDetail] Error loading property {PropertyId}", propertyId);
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

            FormattedPrice = string.Empty;
            HasPrice = false;
            PriceCaption = _loc.PriceCaptionMinimumBid;
            AddressText = string.Empty;
            TypeBadgeText = _loc.TypeBadge;
            DetailSections = [];
            StatTiles = [];
            HasStatTiles = false;
            Description = null;
            HasDescription = false;
            HasDocuments = false;
            _fullImageUrls = [];
            _previewImageUrls = [];
            CurrentImagePosition = 0;
            SetImages([]);
            CourtName = string.Empty;
            HasCourtName = false;
            return;
        }

        Title = Property.Title;
        TypeBadgeText = _loc.TypeBadge;
        TypeBadgeColor = Color.FromArgb("#DE2A2F");

        // Preis formatieren; 0 € (kein Kaufpreis, nur Schaetzwert/Mindestgebot) ausblenden
        HasPrice = Property.Price > 0;
        FormattedPrice = HasPrice ? PropertyDisplay.Price((decimal)Property.Price) : string.Empty;

        // Volle Adresse
        AddressText = $"{Property.Address}, {Property.PostalCode} {Property.City}";

        // Bilder: angezeigt wird die Vorschau-Variante (bzw. solange das Thumbnail,
        // das die Karte schon geladen hat), die volle Aufloesung nur im Vollbild-Viewer
        ApplyImageVariants();

        // TypeSpecificData parsen und Sektionen aufbauen
        BuildDetailSections();
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

    private void BuildDetailSections()
    {
        if (Property is not { } property) return;

        var items = new List<PropertyDetailItem>();

        // TypeSpecificData parsen
        JsonElement? data = null;
        if (!string.IsNullOrWhiteSpace(Property.TypeSpecificData))
        {
            try
            {
                data = JsonSerializer.Deserialize<JsonElement>(Property.TypeSpecificData, JsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "[ForeclosureDetail] Failed to deserialize TypeSpecificData");
            }
        }

        // Preis-Beschriftung passend zur Quelle des Werts (Sync: Price = MinimumBid ?? EstimatedValue)
        var minimumBid = GetJsonDecimal(data, "MinimumBid");
        var estimatedValue = GetJsonDecimal(data, "EstimatedValue");
        PriceCaption = minimumBid is > 0 ? _loc.PriceCaptionMinimumBid
            : estimatedValue is > 0 ? _loc.PriceCaptionEstimatedValue
            : _loc.PriceCaptionMinimumBid;

        // --- VERSTEIGERUNG (wichtigste Daten zuerst) ---
        items.Add(new PropertyDetailItem(_loc.LabelCreatedAt, Property.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy"), PropertyDataCategory.Versteigerung, true));
        AddJsonDateTime(items, data, "AuctionDate", _loc.LabelAuctionDate, PropertyDataCategory.Versteigerung, true);
        AddJsonDecimalCurrency(items, data, "EstimatedValue", _loc.LabelEstimatedValue, PropertyDataCategory.Versteigerung, true);
        AddJsonDecimalCurrency(items, data, "MinimumBid", _loc.LabelMinimumBid, PropertyDataCategory.Versteigerung, true);
        AddJsonString(items, data, "OwnershipShare", _loc.LabelOwnershipShare, PropertyDataCategory.Versteigerung);

        // --- BASISDATEN ---
        items.Add(new PropertyDetailItem(_loc.LabelPropertyType, _loc.TypeForeclosure, PropertyDataCategory.Basisdaten));
        AddJsonString(items, data, "Category", _loc.LabelCategory, PropertyDataCategory.Basisdaten);
        AddJsonStatus(items, data, PropertyDataCategory.Basisdaten);
        AddIfNotEmpty(items, _loc.LabelPostalCode, Property.PostalCode, PropertyDataCategory.Basisdaten);
        AddIfNotEmpty(items, _loc.LabelCity, Property.City, PropertyDataCategory.Basisdaten);
        AddIfNotEmpty(items, _loc.LabelAddress, Property.Address, PropertyDataCategory.Basisdaten);

        // --- RECHTLICHES ---
        var courtName = GetJsonString(data, "CourtName");
        if (!string.IsNullOrWhiteSpace(courtName))
        {
            items.Add(new PropertyDetailItem(_loc.LabelCourt, courtName, PropertyDataCategory.Rechtliches));
            CourtName = courtName;
            HasCourtName = true;
        }
        AddJsonString(items, data, "FileNumber", _loc.LabelFileNumber, PropertyDataCategory.Rechtliches);

        // --- FLÄCHEN ---
        AddJsonDecimalArea(items, data, "TotalArea", _loc.LabelTotalArea, PropertyDataCategory.Flaechen);
        AddIfHasValue(items, _loc.LabelPlot, Property.PlotAreaM2, v => PropertyDisplay.Area(v), PropertyDataCategory.Flaechen);
        AddJsonDecimalArea(items, data, "BuildingArea", _loc.LabelBuildingArea, PropertyDataCategory.Flaechen);

        // --- GRUNDBUCH ---
        AddJsonString(items, data, "RegistrationNumber", _loc.LabelRegistrationNumber, PropertyDataCategory.Grundbuch);
        AddJsonString(items, data, "CadastralMunicipality", _loc.LabelCadastralMunicipality, PropertyDataCategory.Grundbuch);
        AddJsonString(items, data, "PlotNumber", _loc.LabelPlotNumber, PropertyDataCategory.Grundbuch);
        AddJsonString(items, data, "ZoningDesignation", _loc.LabelZoningDesignation, PropertyDataCategory.Grundbuch);

        // --- GEBÄUDE (optional) ---
        if (data.HasValue)
        {
            if (data.Value.TryGetProperty("NumberOfRooms", out var rooms) && rooms.ValueKind == JsonValueKind.Number && rooms.TryGetInt32(out var r) && r > 0)
            {
                items.Add(new PropertyDetailItem(_loc.LabelRooms, r.ToString(), PropertyDataCategory.Gebaeude));
            }
            if (data.Value.TryGetProperty("YearBuilt", out var yb) && yb.ValueKind == JsonValueKind.Number && yb.TryGetInt32(out var y) && y > 0)
            {
                items.Add(new PropertyDetailItem(_loc.LabelYearBuilt, y.ToString(), PropertyDataCategory.Gebaeude));
            }
            AddJsonString(items, data, "BuildingCondition", _loc.LabelCondition, PropertyDataCategory.Gebaeude);
        }

        // --- TERMINE ---
        AddJsonDateTime(items, data, "ViewingDate", _loc.LabelViewingDate, PropertyDataCategory.Termine);
        AddJsonDateTime(items, data, "BiddingDeadline", _loc.LabelBiddingDeadline, PropertyDataCategory.Termine);

        // Beschreibung / Notizen
        var notes = GetJsonString(data, "Notes");
        if (!string.IsNullOrWhiteSpace(notes))
        {
            Description = notes;
            HasDescription = true;
        }
        else if (!string.IsNullOrWhiteSpace(Property.Description))
        {
            Description = Property.Description;
            HasDescription = true;
        }
        else
        {
            Description = null;
            HasDescription = false;
        }

        // Dokumente
        EdictUrl = GetJsonString(data, "EdictUrl");
        FloorPlanUrl = GetJsonString(data, "FloorPlanUrl");
        SitePlanUrl = GetJsonString(data, "SitePlanUrl");
        LongAppraisalUrl = GetJsonString(data, "LongAppraisalUrl");
        ShortAppraisalUrl = GetJsonString(data, "ShortAppraisalUrl");

        HasEdictUrl = !string.IsNullOrEmpty(EdictUrl);
        HasFloorPlanUrl = !string.IsNullOrEmpty(FloorPlanUrl);
        HasSitePlanUrl = !string.IsNullOrEmpty(SitePlanUrl);
        HasLongAppraisalUrl = !string.IsNullOrEmpty(LongAppraisalUrl);
        HasShortAppraisalUrl = !string.IsNullOrEmpty(ShortAppraisalUrl);

        // Edikt hat eine eigene Quellenkarte; DOKUMENTE zeigt nur die uebrigen Anhaenge
        HasDocuments = HasFloorPlanUrl || HasSitePlanUrl || HasLongAppraisalUrl || HasShortAppraisalUrl;

        // Nach Kategorie gruppieren mit eigener Reihenfolge (Versteigerung zuerst)
        var categoryOrder = new[]
        {
            PropertyDataCategory.Versteigerung,
            PropertyDataCategory.Basisdaten,
            PropertyDataCategory.Rechtliches,
            PropertyDataCategory.Flaechen,
            PropertyDataCategory.Grundbuch,
            PropertyDataCategory.Gebaeude,
            PropertyDataCategory.Termine,
            PropertyDataCategory.Sonstiges
        };

        var sections = items
            .GroupBy(i => i.Category)
            .OrderBy(g => Array.IndexOf(categoryOrder, g.Key) is var idx && idx >= 0 ? idx : 999)
            .Select(g => new PropertyDetailSection(GetCategoryTitle(g.Key), g.Key, g.ToList()))
            .ToList();

        var tiles = BuildStatTiles(data, minimumBid, estimatedValue);

        // Erst nach dem naechsten Dispatcher-Durchlauf setzen: die Datentabelle legt
        // acht Karten mit mehreren Dutzend Zeilen ins Layout - Kopf und Foto sollen
        // vorher gezeichnet sein. Das Aufbauen der Eintraege oben bleibt bewusst
        // synchron (reine JsonElement-Zugriffe, im Vergleich zum Layout vernachlaessigbar).
        var propertyId = property.Id;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Zwischenzeitlicher Wechsel auf eine andere Versteigerung darf keine
            // fremde Tabelle einspielen
            if (Property?.Id != propertyId)
                return;

            DetailSections = sections;
            StatTiles = tiles;
            HasStatTiles = tiles.Count > 0;
        });
    }

    /// <summary>
    /// Kernfakten fuer die Kachel-Zeile unter dem Kopf: Termin, Schaetzwert (sofern er
    /// nicht ohnehin schon als prominenter Preis dient) und Gesamtflaeche.
    /// </summary>
    private List<StatTileItem> BuildStatTiles(JsonElement? data, decimal? minimumBid, decimal? estimatedValue)
    {
        var tiles = new List<StatTileItem>();

        if (data.HasValue && data.Value.TryGetProperty("AuctionDate", out var auctionProp)
            && auctionProp.ValueKind == JsonValueKind.String
            && DateTime.TryParse(auctionProp.GetString(), out var auctionDate))
        {
            tiles.Add(new StatTileItem(_loc.TileAuctionDate, auctionDate.ToLocalTime().ToString("dd.MM.yyyy")));
        }

        if (minimumBid is > 0 && estimatedValue is > 0)
            tiles.Add(new StatTileItem(_loc.TileEstimatedValue, PropertyDisplay.Price(estimatedValue.Value)));

        var totalArea = GetJsonDecimal(data, "TotalArea") ?? Property?.PlotAreaM2;
        if (totalArea is > 0)
            tiles.Add(new StatTileItem(_loc.TileArea, PropertyDisplay.Area(totalArea.Value)));

        return tiles;
    }

    #region JSON Helpers

    private static string? GetJsonString(JsonElement? data, string propertyName)
    {
        if (data.HasValue && data.Value.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
            return prop.GetString();
        return null;
    }

    private static decimal? GetJsonDecimal(JsonElement? data, string propertyName)
    {
        if (data.HasValue && data.Value.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number && prop.TryGetDecimal(out var value))
            return value;
        return null;
    }

    private static void AddJsonString(List<PropertyDetailItem> items, JsonElement? data, string propertyName, string label, PropertyDataCategory category, bool highlighted = false)
    {
        var value = GetJsonString(data, propertyName);
        if (!string.IsNullOrWhiteSpace(value))
            items.Add(new PropertyDetailItem(label, value, category, highlighted));
    }

    private static void AddJsonDateTime(List<PropertyDetailItem> items, JsonElement? data, string propertyName, string label, PropertyDataCategory category, bool highlighted = false)
    {
        if (data.HasValue && data.Value.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            if (DateTime.TryParse(prop.GetString(), out var dt))
                items.Add(new PropertyDetailItem(label, dt.ToLocalTime().ToString("dd.MM.yyyy HH:mm"), category, highlighted));
        }
    }

    private static void AddJsonDecimalCurrency(List<PropertyDetailItem> items, JsonElement? data, string propertyName, string label, PropertyDataCategory category, bool highlighted = false)
    {
        if (data.HasValue && data.Value.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number && prop.TryGetDecimal(out var val))
            items.Add(new PropertyDetailItem(label, PropertyDisplay.Price(val), category, highlighted));
    }

    private static void AddJsonDecimalArea(List<PropertyDetailItem> items, JsonElement? data, string propertyName, string label, PropertyDataCategory category)
    {
        if (data.HasValue && data.Value.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number && prop.TryGetDecimal(out var val) && val > 0)
            items.Add(new PropertyDetailItem(label, PropertyDisplay.Area(val), category));
    }

    private void AddJsonStatus(List<PropertyDetailItem> items, JsonElement? data, PropertyDataCategory category, bool highlighted = false)
    {
        if (data.HasValue && data.Value.TryGetProperty("Status", out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            var statusValue = prop.GetString() ?? "";
            items.Add(new PropertyDetailItem(_loc.LabelStatus, PropertyDisplay.LegalStatusText(statusValue), category, highlighted));
        }
    }

    #endregion

    #region Formatting Helpers

    private string GetCategoryTitle(PropertyDataCategory category) => category switch
    {
        PropertyDataCategory.Basisdaten => _loc.SectionBasicData,
        PropertyDataCategory.Flaechen => _loc.SectionAreas,
        PropertyDataCategory.Gebaeude => _loc.SectionBuilding,
        PropertyDataCategory.Grundbuch => _loc.SectionLandRegistry,
        PropertyDataCategory.Versteigerung => _loc.SectionAuction,
        PropertyDataCategory.Rechtliches => _loc.SectionLegal,
        PropertyDataCategory.Termine => _loc.SectionDates,
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
        if (Images.Count == 0)
            return;
        CurrentImagePosition = CurrentImagePosition <= 0 ? Images.Count - 1 : CurrentImagePosition - 1;
    }

    [RelayCommand]
    private void ShowNextImage()
    {
        if (Images.Count == 0)
            return;
        CurrentImagePosition = CurrentImagePosition >= Images.Count - 1 ? 0 : CurrentImagePosition + 1;
    }

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

        _logger.LogInformation("[ForeclosureDetail] Toggling favorite for {PropertyId}", Property.Id);

        IsFavorite = await _propertyStatusService.ToggleFavoriteAsync(Property.Id);
    }

    /// <summary>
    /// Teilt die Zwangsversteigerung ueber nativen Share-Dialog oder Zwischenablage
    /// </summary>
    [RelayCommand]
    private async Task SharePropertyAsync()
    {
        if (Property == null)
            return;

        _logger.LogInformation("[ForeclosureDetail] Sharing property {PropertyId}", Property.Id);

        // MAUI zeigt hier eine Property vom Typ Foreclosure. Die separate
        // /zwangsversteigerungen/-Route ist hingegen fuer ForeclosureAuction-Datensaetze
        // aus dem Edikte-Sync bestimmt und kennt diese Property-GUID nicht.
        // Web-Pendant des aktiven Endpunkts: Debug/Test-Builds teilen Test-Links
        var propertyUrl = Services.WebLinks.ListingUrl(_apiEndpoints.CurrentUrl, Property.Id);

        var description = HasPrice
            ? _loc.ShareDescriptionFormat(Property.Title, PriceCaption, FormattedPrice, AddressText)
            : _loc.ShareDescriptionNoPriceFormat(Property.Title, AddressText);

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
    /// Oeffnet die Edikt-Seite im Browser
    /// </summary>
    [RelayCommand]
    private async Task OpenEdictAsync()
    {
        if (string.IsNullOrWhiteSpace(EdictUrl))
            return;

        _logger.LogInformation("[ForeclosureDetail] Opening edict: {Url}", EdictUrl);
        await Launcher.Default.OpenAsync(new Uri(EdictUrl));
    }

    /// <summary>
    /// Oeffnet ein Dokument im Browser
    /// </summary>
    [RelayCommand]
    private async Task OpenDocumentAsync(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        _logger.LogInformation("[ForeclosureDetail] Opening document: {Url}", url);
        await Launcher.Default.OpenAsync(new Uri(url));
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
