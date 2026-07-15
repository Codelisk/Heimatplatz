using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Features.Auth;
using Heimatplatz.Maui.Features.Properties.Models;
using Heimatplatz.Maui.Features.Properties.Services;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

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
    private readonly ILogger<ForeclosureDetailViewModel> _logger;

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

    /// <summary>True wenn die Zwangsversteigerung nicht geladen werden konnte (Fehler oder geloescht)</summary>
    [ObservableProperty]
    public partial bool HasLoadError { get; set; }

    [ObservableProperty]
    public partial string? LoadErrorText { get; set; }

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
    /// Text fuer den Favoriten-Button je nach Status
    /// </summary>
    public string FavoriteButtonText => IsFavorite ? "Gemerkt" : "Merken";

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
        ILogger<ForeclosureDetailViewModel> logger)
    {
        _clipboardService = clipboardService;
        _shareService = shareService;
        _mediator = mediator;
        _authService = authService;
        _propertyStatusService = propertyStatusService;
        _logger = logger;

        Title = "Zwangsversteigerung";
        FormattedPrice = string.Empty;
        AddressText = string.Empty;
        TypeBadgeText = "ZV";
        TypeBadgeColor = Color.FromArgb("#B22222");
        DetailSections = [];
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
    }

    #endregion

    private async Task LoadPropertyAsync(Guid propertyId)
    {
        IsBusy = true;
        BusyMessage = "Lade Zwangsversteigerung...";
        HasLoadError = false;
        LoadErrorText = null;

        try
        {
            _logger.LogInformation("[ForeclosureDetail] Loading property {PropertyId} from API", propertyId);

            var (_, response) = await _mediator.Request(new GetPropertyByIdHttpRequest { Id = propertyId });

            if (response?.Property != null)
            {
                Property = response.Property;
                _logger.LogInformation("[ForeclosureDetail] Property loaded: {Title}", Property.Title);
            }
            else
            {
                Property = null;
                _logger.LogWarning("[ForeclosureDetail] Property {PropertyId} not found", propertyId);
                HasLoadError = true;
                LoadErrorText = "Diese Zwangsversteigerung ist nicht mehr verfügbar.";
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ForeclosureDetail] Error loading property {PropertyId}", propertyId);
            Property = null;
            UpdateDisplayProperties();
            HasLoadError = true;
            LoadErrorText = "Die Zwangsversteigerung konnte nicht geladen werden. Bitte überprüfen Sie Ihre Internetverbindung und versuchen Sie es erneut.";
        }
        finally
        {
            IsBusy = false;
            BusyMessage = null;
        }
    }

    private void UpdateDisplayProperties()
    {
        if (Property == null)
        {
            FormattedPrice = string.Empty;
            HasPrice = false;
            AddressText = string.Empty;
            TypeBadgeText = "ZV";
            DetailSections = [];
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
        TypeBadgeText = "ZV";
        TypeBadgeColor = Color.FromArgb("#B22222");

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

        // --- VERSTEIGERUNG (wichtigste Daten zuerst) ---
        items.Add(new PropertyDetailItem("Eingestellt am", Property.CreatedAt.ToString("dd.MM.yyyy"), PropertyDataCategory.Versteigerung, true));
        AddJsonDateTime(items, data, "AuctionDate", "Versteigerungstermin", PropertyDataCategory.Versteigerung, true);
        AddJsonDecimalCurrency(items, data, "EstimatedValue", "Schätzwert", PropertyDataCategory.Versteigerung, true);
        AddJsonDecimalCurrency(items, data, "MinimumBid", "Mindestgebot", PropertyDataCategory.Versteigerung, true);
        AddJsonString(items, data, "OwnershipShare", "Eigentumsanteil", PropertyDataCategory.Versteigerung);

        // --- BASISDATEN ---
        items.Add(new PropertyDetailItem("Immobilienart", "Zwangsversteigerung", PropertyDataCategory.Basisdaten));
        AddJsonString(items, data, "Category", "Kategorie", PropertyDataCategory.Basisdaten);
        AddJsonStatus(items, data, PropertyDataCategory.Basisdaten);
        AddIfNotEmpty(items, "PLZ", Property.PostalCode, PropertyDataCategory.Basisdaten);
        AddIfNotEmpty(items, "Ort", Property.City, PropertyDataCategory.Basisdaten);
        AddIfNotEmpty(items, "Adresse", Property.Address, PropertyDataCategory.Basisdaten);

        // --- RECHTLICHES ---
        var courtName = GetJsonString(data, "CourtName");
        if (!string.IsNullOrWhiteSpace(courtName))
        {
            items.Add(new PropertyDetailItem("Gericht", courtName, PropertyDataCategory.Rechtliches));
            CourtName = courtName;
            HasCourtName = true;
        }
        AddJsonString(items, data, "FileNumber", "Aktenzeichen", PropertyDataCategory.Rechtliches);

        // --- FLÄCHEN ---
        AddJsonDecimalArea(items, data, "TotalArea", "Gesamtfläche", PropertyDataCategory.Flaechen);
        AddIfHasValue(items, "Grundstück", Property.PlotAreaM2, v => PropertyDisplay.Area(v), PropertyDataCategory.Flaechen);
        AddJsonDecimalArea(items, data, "BuildingArea", "Bebaute Fläche", PropertyDataCategory.Flaechen);

        // --- GRUNDBUCH ---
        AddJsonString(items, data, "RegistrationNumber", "Einlagezahl (EZ)", PropertyDataCategory.Grundbuch);
        AddJsonString(items, data, "CadastralMunicipality", "Katastralgemeinde", PropertyDataCategory.Grundbuch);
        AddJsonString(items, data, "PlotNumber", "Grundstücksnummer", PropertyDataCategory.Grundbuch);
        AddJsonString(items, data, "ZoningDesignation", "Flächenwidmung", PropertyDataCategory.Grundbuch);

        // --- GEBÄUDE (optional) ---
        if (data.HasValue)
        {
            if (data.Value.TryGetProperty("NumberOfRooms", out var rooms) && rooms.ValueKind == JsonValueKind.Number && rooms.TryGetInt32(out var r) && r > 0)
            {
                items.Add(new PropertyDetailItem("Zimmer", r.ToString(), PropertyDataCategory.Gebaeude));
            }
            if (data.Value.TryGetProperty("YearBuilt", out var yb) && yb.ValueKind == JsonValueKind.Number && yb.TryGetInt32(out var y) && y > 0)
            {
                items.Add(new PropertyDetailItem("Baujahr", y.ToString(), PropertyDataCategory.Gebaeude));
            }
            AddJsonString(items, data, "BuildingCondition", "Zustand", PropertyDataCategory.Gebaeude);
        }

        // --- TERMINE ---
        AddJsonDateTime(items, data, "ViewingDate", "Besichtigung", PropertyDataCategory.Termine);
        AddJsonDateTime(items, data, "BiddingDeadline", "Gebotsfrist", PropertyDataCategory.Termine);

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
    }

    #region JSON Helpers

    private static string? GetJsonString(JsonElement? data, string propertyName)
    {
        if (data.HasValue && data.Value.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
            return prop.GetString();
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

    private static void AddJsonStatus(List<PropertyDetailItem> items, JsonElement? data, PropertyDataCategory category, bool highlighted = false)
    {
        if (data.HasValue && data.Value.TryGetProperty("Status", out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            var statusValue = prop.GetString() ?? "";
            items.Add(new PropertyDetailItem("Status", PropertyDisplay.LegalStatusText(statusValue), category, highlighted));
        }
    }

    #endregion

    #region Formatting Helpers

    private static string GetCategoryTitle(PropertyDataCategory category) => category switch
    {
        PropertyDataCategory.Basisdaten => "BASISDATEN",
        PropertyDataCategory.Flaechen => "FLÄCHEN",
        PropertyDataCategory.Gebaeude => "GEBÄUDE",
        PropertyDataCategory.Grundbuch => "GRUNDBUCH",
        PropertyDataCategory.Versteigerung => "VERSTEIGERUNG",
        PropertyDataCategory.Rechtliches => "RECHTLICHES",
        PropertyDataCategory.Termine => "TERMINE",
        PropertyDataCategory.Sonstiges => "SONSTIGES",
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
            await ShowCopyFeedbackAsync("Kopiert!", 1500);
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

        var propertyUrl = new Uri($"https://heimatplatz.at/zwangsversteigerung/{Property.Id}");

        var description = $"Zwangsversteigerung: {Property.Title}\n" +
                          $"Preis: {FormattedPrice}\n" +
                          $"Standort: {AddressText}";

        var result = await _shareService.ShareLinkAsync(Property.Title, propertyUrl, description);
        if (result == ShareResult.SharedNatively)
        {
            await ShowCopyFeedbackAsync("Geteilt!", 2000);
        }
        else if (result == ShareResult.CopiedToClipboard)
        {
            await ShowCopyFeedbackAsync("In Zwischenablage kopiert!", 2000);
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
