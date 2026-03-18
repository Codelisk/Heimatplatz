using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using UnoFramework.Contracts.Navigation;
using UnoFramework.Contracts.Pages;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Models;
using Microsoft.Extensions.Logging;
using Shiny.Mediator;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// ViewModel fuer die ForeclosureDetailPage (Immoscout Style)
/// Registered via Uno.Extensions.Navigation ViewMap (not [Service] attribute)
/// </summary>
public partial class ForeclosureDetailViewModel : ObservableObject, IPageInfo, INavigationAware
{
    private readonly IClipboardService _clipboardService;
    private readonly IShareService _shareService;
    private readonly IMediator _mediator;
    private readonly IAuthService _authService;
    private readonly IPropertyStatusService _propertyStatusService;
    private readonly ILogger<ForeclosureDetailViewModel> _logger;
    private readonly Guid _propertyId;
    private DispatcherQueue? _dispatcher;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _busyMessage;

    [ObservableProperty]
    private PropertyDetailDto? _property;

    [ObservableProperty]
    private int _currentImageIndex = 1;

    [ObservableProperty]
    private string _formattedPrice = string.Empty;

    [ObservableProperty]
    private string _addressText = string.Empty;

    [ObservableProperty]
    private string? _copyFeedback;

    [ObservableProperty]
    private bool _isAuthenticated;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FavoriteButtonText))]
    [NotifyPropertyChangedFor(nameof(FavoriteButtonIcon))]
    private bool _isFavorite;

    [ObservableProperty]
    private string _typeBadgeText = "ZV";

    [ObservableProperty]
    private Microsoft.UI.Xaml.Media.Brush? _typeBadgeBrush;

    [ObservableProperty]
    private List<PropertyDetailSection> _detailSections = [];

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private bool _hasDescription;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ContactExpandIcon))]
    private bool _isContactExpanded;

    // === Document URLs ===

    [ObservableProperty]
    private string? _edictUrl;

    [ObservableProperty]
    private string? _floorPlanUrl;

    [ObservableProperty]
    private string? _sitePlanUrl;

    [ObservableProperty]
    private string? _longAppraisalUrl;

    [ObservableProperty]
    private string? _shortAppraisalUrl;

    [ObservableProperty]
    private bool _hasDocuments;

    [ObservableProperty]
    private bool _hasImages;

    // === Seller info for contact box ===

    [ObservableProperty]
    private string _courtName = string.Empty;

    /// <summary>
    /// Chevron icon: up when collapsed, down when expanded
    /// </summary>
    public string ContactExpandIcon => IsContactExpanded ? "\uE70D" : "\uE70E";

    /// <summary>
    /// Text for the favorite button based on current status
    /// </summary>
    public string FavoriteButtonText => IsFavorite ? "Gemerkt" : "Merken";

    /// <summary>
    /// Icon glyph for the favorite button (filled/outline heart)
    /// </summary>
    public string FavoriteButtonIcon => IsFavorite ? "\uEB52" : "\uEB51";

    #region IPageInfo Implementation

    public PageType PageType => PageType.Detail;
    public string PageTitle => "Zwangsversteigerung";
    public Type? MainHeaderViewModel => null;

    #endregion

    public ForeclosureDetailViewModel(
        IClipboardService clipboardService,
        IShareService shareService,
        IMediator mediator,
        IAuthService authService,
        IPropertyStatusService propertyStatusService,
        ILogger<ForeclosureDetailViewModel> logger,
        ForeclosureDetailData data)
    {
        _clipboardService = clipboardService;
        _shareService = shareService;
        _mediator = mediator;
        _authService = authService;
        _propertyStatusService = propertyStatusService;
        _logger = logger;
        _propertyId = data.PropertyId;
        _isAuthenticated = authService.IsAuthenticated;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private void UpdateDisplayProperties()
    {
        if (Property == null)
        {
            FormattedPrice = string.Empty;
            AddressText = string.Empty;
            TypeBadgeText = "ZV";
            DetailSections = [];
            Description = null;
            HasDescription = false;
            HasDocuments = false;
            HasImages = false;
            CourtName = string.Empty;
            return;
        }

        // Type badge
        TypeBadgeText = "ZV";
        TypeBadgeBrush = GetThemeBrush("ZwangsversteigerungBrush");

        // Format price
        FormattedPrice = $"{Property.Price:N0} €".Replace(",", ".");

        // Full address
        AddressText = $"{Property.Address}, {Property.PostalCode} {Property.City}";

        // Images
        HasImages = Property.ImageUrls is { Count: > 0 };

        // Parse TypeSpecificData and build sections
        BuildDetailSections();
    }

    private void BuildDetailSections()
    {
        if (Property == null) return;

        var items = new List<PropertyDetailItem>();

        // Parse TypeSpecificData
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
        AddJsonDecimalCurrency(items, data, "EstimatedValue", "Schaetzwert", PropertyDataCategory.Versteigerung, true);
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
        }
        AddJsonString(items, data, "FileNumber", "Aktenzeichen", PropertyDataCategory.Rechtliches);

        // --- FLÄCHEN ---
        AddJsonDecimalArea(items, data, "TotalArea", "Gesamtfläche", PropertyDataCategory.Flaechen);
        AddIfHasValue(items, "Grundstück", Property.PlotAreaM2, v => $"{v:N0} m\u00B2".Replace(",", "."), PropertyDataCategory.Flaechen);
        AddJsonDecimalArea(items, data, "BuildingArea", "Bebaute Fläche", PropertyDataCategory.Flaechen);

        // --- GRUNDBUCH ---
        AddJsonString(items, data, "RegistrationNumber", "Einlagezahl (EZ)", PropertyDataCategory.Grundbuch);
        AddJsonString(items, data, "CadastralMunicipality", "Katastralgemeinde", PropertyDataCategory.Grundbuch);
        AddJsonString(items, data, "PlotNumber", "Grundstücksnummer", PropertyDataCategory.Grundbuch);
        AddJsonString(items, data, "ZoningDesignation", "Flächenwidmung", PropertyDataCategory.Grundbuch);

        // --- GEBÄUDE (conditional) ---
        var hasRoomData = false;
        if (data.HasValue)
        {
            if (data.Value.TryGetProperty("NumberOfRooms", out var rooms) && rooms.ValueKind == JsonValueKind.Number && rooms.TryGetInt32(out var r) && r > 0)
            {
                items.Add(new PropertyDetailItem("Zimmer", r.ToString(), PropertyDataCategory.Gebaeude));
                hasRoomData = true;
            }
            if (data.Value.TryGetProperty("YearBuilt", out var yb) && yb.ValueKind == JsonValueKind.Number && yb.TryGetInt32(out var y) && y > 0)
            {
                items.Add(new PropertyDetailItem("Baujahr", y.ToString(), PropertyDataCategory.Gebaeude));
                hasRoomData = true;
            }
            AddJsonString(items, data, "BuildingCondition", "Zustand", PropertyDataCategory.Gebaeude);
            if (!hasRoomData)
            {
                if (items.Any(i => i.Category == PropertyDataCategory.Gebaeude))
                    hasRoomData = true;
            }
        }

        // --- TERMINE ---
        AddJsonDateTime(items, data, "ViewingDate", "Besichtigung", PropertyDataCategory.Termine);
        AddJsonDateTime(items, data, "BiddingDeadline", "Gebotsfrist", PropertyDataCategory.Termine);

        // Description / Notes
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

        // Documents
        EdictUrl = GetJsonString(data, "EdictUrl");
        FloorPlanUrl = GetJsonString(data, "FloorPlanUrl");
        SitePlanUrl = GetJsonString(data, "SitePlanUrl");
        LongAppraisalUrl = GetJsonString(data, "LongAppraisalUrl");
        ShortAppraisalUrl = GetJsonString(data, "ShortAppraisalUrl");

        HasDocuments = !string.IsNullOrEmpty(EdictUrl) ||
                       !string.IsNullOrEmpty(FloorPlanUrl) ||
                       !string.IsNullOrEmpty(SitePlanUrl) ||
                       !string.IsNullOrEmpty(LongAppraisalUrl) ||
                       !string.IsNullOrEmpty(ShortAppraisalUrl);

        // Group by category with custom order (Versteigerung first)
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
        if (data.HasValue && data.Value.TryGetProperty(propertyName, out var prop))
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
        if (data.HasValue && data.Value.TryGetProperty(propertyName, out var prop))
        {
            if (DateTime.TryParse(prop.GetString(), out var dt))
                items.Add(new PropertyDetailItem(label, dt.ToString("dd.MM.yyyy HH:mm"), category, highlighted));
        }
    }

    private static void AddJsonDecimalCurrency(List<PropertyDetailItem> items, JsonElement? data, string propertyName, string label, PropertyDataCategory category, bool highlighted = false)
    {
        if (data.HasValue && data.Value.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number && prop.TryGetDecimal(out var val))
            items.Add(new PropertyDetailItem(label, $"{val:N0} \u20AC".Replace(",", "."), category, highlighted));
    }

    private static void AddJsonDecimalArea(List<PropertyDetailItem> items, JsonElement? data, string propertyName, string label, PropertyDataCategory category)
    {
        if (data.HasValue && data.Value.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number && prop.TryGetDecimal(out var val) && val > 0)
            items.Add(new PropertyDetailItem(label, $"{val:N0} m\u00B2".Replace(",", "."), category));
    }

    private static void AddJsonStatus(List<PropertyDetailItem> items, JsonElement? data, PropertyDataCategory category, bool highlighted = false)
    {
        if (data.HasValue && data.Value.TryGetProperty("Status", out var prop))
        {
            var statusValue = prop.GetString() ?? "";
            var translated = statusValue switch
            {
                "Pending" => "Anhaengig",
                "Scheduled" => "Termin angesetzt",
                "InProgress" => "Laufend",
                "Completed" => "Abgeschlossen",
                "Cancelled" => "Abgebrochen",
                "Suspended" => "Ausgesetzt",
                _ => statusValue
            };
            items.Add(new PropertyDetailItem("Status", translated, category, highlighted));
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

    public async Task LoadPropertyAsync(Guid propertyId)
    {
        DispatchToUI(() =>
        {
            IsBusy = true;
            BusyMessage = "Lade Zwangsversteigerung...";
        });

        try
        {
            _logger.LogInformation("[ForeclosureDetail] Loading property {PropertyId} from API", propertyId);

            var request = new Heimatplatz.Core.ApiClient.Generated.GetPropertyByIdHttpRequest
            {
                Id = propertyId
            };

            var (context, response) = await _mediator.Request(request);

            PropertyDetailDto? loadedProperty = null;
            if (response?.Property != null)
            {
                var prop = response.Property;
                loadedProperty = new PropertyDetailDto(
                    Id: prop.Id,
                    Title: prop.Title,
                    Address: prop.Address,
                    City: prop.City,
                    PostalCode: prop.PostalCode,
                    Price: (decimal)prop.Price,
                    LivingAreaM2: prop.LivingAreaM2,
                    PlotAreaM2: prop.PlotAreaM2,
                    Rooms: prop.Rooms,
                    YearBuilt: prop.YearBuilt,
                    Type: Enum.Parse<PropertyType>(prop.Type.ToString()),
                    SellerType: Enum.Parse<SellerType>(prop.SellerType.ToString()),
                    SellerName: prop.SellerName,
                    ImageUrls: prop.ImageUrls,
                    Description: prop.Description,
                    Features: prop.Features,
                    InquiryType: Enum.Parse<InquiryType>(prop.InquiryType.ToString()),
                    Contacts: prop.Contacts?.Select(c => new ContactInfoDto(
                        Id: c.Id,
                        Type: Enum.Parse<ContactType>(c.Type.ToString()),
                        Source: Enum.Parse<ContactSource>(c.Source.ToString()),
                        Name: c.Name,
                        Email: c.Email,
                        Phone: c.Phone,
                        OriginalListingUrl: c.OriginalListingUrl,
                        SourceName: c.SourceName,
                        DisplayOrder: c.DisplayOrder
                    )).ToList() ?? [],
                    CreatedAt: prop.CreatedAt,
                    TypeSpecificData: prop.TypeSpecificData
                );

                _logger.LogInformation("[ForeclosureDetail] Property loaded: {Title}", loadedProperty.Title);
            }
            else
            {
                _logger.LogWarning("[ForeclosureDetail] Property {PropertyId} not found", propertyId);
            }

            // Load favorite status
            var isFavorite = false;
            if (_authService.IsAuthenticated)
            {
                await _propertyStatusService.RefreshStatusAsync();
                isFavorite = _propertyStatusService.IsFavorite(propertyId);
            }

            // Update all UI-bound properties on the UI thread
            DispatchToUI(() =>
            {
                Property = loadedProperty;
                UpdateDisplayProperties();
                IsFavorite = isFavorite;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ForeclosureDetail] Error loading property {PropertyId}", propertyId);
        }
        finally
        {
            DispatchToUI(() =>
            {
                IsBusy = false;
                BusyMessage = null;
            });
        }
    }

    /// <summary>
    /// Kopiert einen Text in die Zwischenablage
    /// </summary>
    public async Task CopyToClipboardAsync(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        var success = await _clipboardService.CopyToClipboardAsync(text);
        if (success)
        {
            CopyFeedback = "Kopiert!";
            await Task.Delay(1500);
            CopyFeedback = null;
        }
    }

    /// <summary>
    /// Toggles the favorite status of the current property
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

        var success = await _shareService.ShareLinkAsync(Property.Title, propertyUrl, description);
        if (success)
        {
            CopyFeedback = "Geteilt!";
            await Task.Delay(2000);
            CopyFeedback = null;
        }
    }

    /// <summary>
    /// Toggles the contact detail expansion in the footer bar
    /// </summary>
    [RelayCommand]
    private void ToggleContactExpanded()
    {
        IsContactExpanded = !IsContactExpanded;
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
        await Windows.System.Launcher.LaunchUriAsync(new Uri(EdictUrl));
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
        await Windows.System.Launcher.LaunchUriAsync(new Uri(url));
    }

    #region INavigationAware Implementation

    /// <inheritdoc />
    public void OnNavigatedTo(object? parameter)
    {
        _logger.LogDebug("[ForeclosureDetail] OnNavigatedTo");
        _dispatcher ??= DispatcherQueue.GetForCurrentThread();
        _ = LoadPropertyAsync(_propertyId);
    }

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
        _logger.LogDebug("[ForeclosureDetail] OnNavigatedFrom");
    }

    #endregion

    private void DispatchToUI(Action action)
    {
        var dq = _dispatcher;
        if (dq is not null)
            dq.TryEnqueue(() => action());
        else
            action();
    }

    private static Microsoft.UI.Xaml.Media.Brush GetThemeBrush(string resourceKey)
    {
        if (Microsoft.UI.Xaml.Application.Current.Resources.TryGetValue(resourceKey, out var resource)
            && resource is Microsoft.UI.Xaml.Media.Brush brush)
        {
            return brush;
        }
        return new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray);
    }
}
