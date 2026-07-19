using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Maui.ApiClient.Generated;
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

    [ObservableProperty]
    public partial List<string> ImageUrls { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ImageCounterText))]
    [NotifyPropertyChangedFor(nameof(CurrentImageUrl))]
    public partial int CurrentImagePosition { get; set; }

    [ObservableProperty]
    public partial string CourtName { get; set; }

    [ObservableProperty]
    public partial bool HasCourtName { get; set; }

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

    /// <summary>True bei mehr als einem Bild (blendet die Pfeile im Windows-Bildviewer ein)</summary>
    public bool HasMultipleImages => ImageUrls.Count > 1;

    partial void OnImageUrlsChanged(List<string> value)
    {
        OnPropertyChanged(nameof(CurrentImageUrl));
        OnPropertyChanged(nameof(HasMultipleImages));
        OnPropertyChanged(nameof(ShowViewerNavigation));
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
        ILogger<ForeclosureDetailViewModel> logger,
        ForeclosureDetailStringsLocalized loc)
    {
        _clipboardService = clipboardService;
        _shareService = shareService;
        _mediator = mediator;
        _authService = authService;
        _propertyStatusService = propertyStatusService;
        _internet = internet;
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
        ImageUrls = [];
        CourtName = string.Empty;
        IsAuthenticated = authService.IsAuthenticated;
    }

    #region IPageLifecycleAware

    public void OnAppearing()
    {
        if (Guid.TryParse(PropertyId, out var id))
        {
            _ = LoadPropertyAsync(id);
        }
        else
        {
            _logger.LogWarning("[ForeclosureDetail] Invalid PropertyId: {PropertyId}", PropertyId);
        }
    }

    public void OnDisappearing()
    {
        _onlineWaitCts?.Cancel();
        _onlineWaitCts = null;
    }

    #endregion

    private CancellationTokenSource? _onlineWaitCts;

    private async Task LoadPropertyAsync(Guid propertyId)
    {
        IsBusy = true;
        BusyMessage = _loc.BusyLoading;
        HasLoadError = false;
        LoadErrorText = null;
        _onlineWaitCts?.Cancel();

        try
        {
            _logger.LogInformation("[ForeclosureDetail] Loading property {PropertyId} from API", propertyId);

            var (_, response) = await _mediator.Request(new GetPropertyByIdHttpRequest { Id = propertyId });

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
                    "⚖️",
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
        }
        catch (Exception ex) when (ex is OfflineDataUnavailableException || !_internet.IsAvailable)
        {
            // Kein Internet und (noch) keine lokal gespeicherte Antwort - die Versteigerung
            // existiert weiterhin, sie kann nur gerade nicht geladen werden
            _logger.LogInformation("[ForeclosureDetail] Offline ohne lokale Daten fuer {PropertyId}", propertyId);
            Property = null;
            UpdateDisplayProperties();
            SetOfflineError(propertyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ForeclosureDetail] Error loading property {PropertyId}", propertyId);
            Property = null;
            UpdateDisplayProperties();
            SetLoadError(
                "📡",
                _loc.LoadFailedTitle,
                ex is HttpRequestException
                    ? _loc.ServerUnreachableText
                    : _loc.TryAgainLaterText,
                canRetry: true);
        }
        finally
        {
            IsBusy = false;
            BusyMessage = null;
        }
    }

    private void SetOfflineError(Guid propertyId)
    {
        SetLoadError(
            "📡",
            _loc.OfflineTitle,
            _loc.OfflineText,
            canRetry: true);
        StartAutoReloadWhenOnline(propertyId);
    }

    private void SetLoadError(string icon, string title, string text, bool canRetry)
    {
        LoadErrorIcon = icon;
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
            HasImages = false;
            ImageUrls = [];
            CurrentImagePosition = 0;
            OnPropertyChanged(nameof(ImageCounterText));
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

        // Bilder
        ImageUrls = Property.ImageUrls?.Where(url => !string.IsNullOrEmpty(url)).ToList() ?? [];
        HasImages = ImageUrls.Count > 0;
        CurrentImagePosition = 0;
        OnPropertyChanged(nameof(ImageCounterText));

        // TypeSpecificData parsen und Sektionen aufbauen
        BuildDetailSections();
    }

    private void BuildDetailSections()
    {
        if (Property == null) return;

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
        items.Add(new PropertyDetailItem(_loc.LabelCreatedAt, Property.CreatedAt.ToString("dd.MM.yyyy"), PropertyDataCategory.Versteigerung, true));
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

        DetailSections = items
            .GroupBy(i => i.Category)
            .OrderBy(g => Array.IndexOf(categoryOrder, g.Key) is var idx && idx >= 0 ? idx : 999)
            .Select(g => new PropertyDetailSection(GetCategoryTitle(g.Key), g.Key, g.ToList()))
            .ToList();

        BuildStatTiles(data, minimumBid, estimatedValue);
    }

    /// <summary>
    /// Kernfakten fuer die Kachel-Zeile unter dem Kopf: Termin, Schaetzwert (sofern er
    /// nicht ohnehin schon als prominenter Preis dient) und Gesamtflaeche.
    /// </summary>
    private void BuildStatTiles(JsonElement? data, decimal? minimumBid, decimal? estimatedValue)
    {
        var tiles = new List<StatTileItem>();

        if (data.HasValue && data.Value.TryGetProperty("AuctionDate", out var auctionProp)
            && auctionProp.ValueKind == JsonValueKind.String
            && DateTime.TryParse(auctionProp.GetString(), out var auctionDate))
        {
            tiles.Add(new StatTileItem(_loc.TileAuctionDate, auctionDate.ToString("dd.MM.yy")));
        }

        if (minimumBid is > 0 && estimatedValue is > 0)
            tiles.Add(new StatTileItem(_loc.TileEstimatedValue, PropertyDisplay.Price(estimatedValue.Value)));

        var totalArea = GetJsonDecimal(data, "TotalArea") ?? Property?.PlotAreaM2;
        if (totalArea is > 0)
            tiles.Add(new StatTileItem(_loc.TileArea, PropertyDisplay.Area(totalArea.Value)));

        StatTiles = tiles;
        HasStatTiles = tiles.Count > 0;
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
                items.Add(new PropertyDetailItem(label, dt.ToString("dd.MM.yyyy HH:mm"), category, highlighted));
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
        var propertyUrl = new Uri($"https://heimatplatz.at/immobilien/angebote/{Property.Id}");

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
