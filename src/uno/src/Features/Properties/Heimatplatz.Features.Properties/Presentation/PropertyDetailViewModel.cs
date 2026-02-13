using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using UnoFramework.Contracts.Navigation;
using UnoFramework.Contracts.Pages;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Models;
using Heimatplatz.Features.Properties.Contracts.Models.TypeSpecific;
using Heimatplatz.Features.Properties.Contracts.Models.TypeSpecific.Enums;
using Microsoft.Extensions.Logging;
using Shiny.Mediator;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// ViewModel fuer die PropertyDetailPage (Immoscout Style)
/// Registered via Uno.Extensions.Navigation ViewMap (not [Service] attribute)
/// </summary>
public partial class PropertyDetailViewModel : ObservableObject, IPageInfo, INavigationAware
{
    private readonly IClipboardService _clipboardService;
    private readonly IShareService _shareService;
    private readonly IMediator _mediator;
    private readonly IAuthService _authService;
    private readonly IPropertyStatusService _propertyStatusService;
    private readonly ILogger<PropertyDetailViewModel> _logger;
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
    private string _pricePerSqmText = "-";

    [ObservableProperty]
    private string _plotAreaText = "-";

    [ObservableProperty]
    private string _yearBuiltText = "-";

    [ObservableProperty]
    private string _addressText = string.Empty;

    [ObservableProperty]
    private string _contactPersonText = string.Empty;

    [ObservableProperty]
    private bool _hasContactPerson;

    [ObservableProperty]
    private string? _copyFeedback;

    [ObservableProperty]
    private bool _isAuthenticated;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FavoriteButtonText))]
    [NotifyPropertyChangedFor(nameof(FavoriteButtonIcon))]
    private bool _isFavorite;

    [ObservableProperty]
    private bool _isHouseType;

    [ObservableProperty]
    private string _typeBadgeText = string.Empty;

    [ObservableProperty]
    private Microsoft.UI.Xaml.Media.Brush? _typeBadgeBrush;

    [ObservableProperty]
    private List<PropertyDetailSection> _detailSections = [];

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private bool _hasDescription;

    [ObservableProperty]
    private List<string> _featuresList = [];

    [ObservableProperty]
    private bool _hasFeatures;

    [ObservableProperty]
    private bool _isBroker;

    [ObservableProperty]
    private string? _originalListingUrl;

    [ObservableProperty]
    private string? _primaryContactEmail;

    [ObservableProperty]
    private string? _primaryContactPhone;

    [ObservableProperty]
    private bool _hasPrimaryEmail;

    [ObservableProperty]
    private bool _hasPrimaryPhone;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ContactExpandIcon))]
    private bool _isContactExpanded;

    /// <summary>
    /// Chevron icon: up when collapsed (to indicate "expand"), down when expanded (to indicate "collapse")
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
    public string PageTitle => Property?.Title ?? "Immobilie";
    public Type? MainHeaderViewModel => null;

    #endregion

    public PropertyDetailViewModel(
        IClipboardService clipboardService,
        IShareService shareService,
        IMediator mediator,
        IAuthService authService,
        IPropertyStatusService propertyStatusService,
        ILogger<PropertyDetailViewModel> logger,
        PropertyDetailData data)
    {
        _clipboardService = clipboardService;
        _shareService = shareService;
        _mediator = mediator;
        _authService = authService;
        _propertyStatusService = propertyStatusService;
        _logger = logger;
        _propertyId = data.PropertyId;
        _isAuthenticated = authService.IsAuthenticated;

        // Property data is loaded in OnNavigatedTo (guaranteed UI thread for dispatcher access)
    }

    /// <summary>
    /// Gibt an, ob Kontaktdaten verfuegbar sind
    /// </summary>
    public bool HasContacts => Property?.Contacts?.Count > 0;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private void UpdateDisplayProperties()
    {
        if (Property == null)
        {
            FormattedPrice = string.Empty;
            PricePerSqmText = "-";
            PlotAreaText = "-";
            YearBuiltText = "-";
            AddressText = string.Empty;
            ContactPersonText = string.Empty;
            HasContactPerson = false;
            IsHouseType = false;
            IsBroker = false;
            OriginalListingUrl = null;
            PrimaryContactEmail = null;
            PrimaryContactPhone = null;
            HasPrimaryEmail = false;
            HasPrimaryPhone = false;
            TypeBadgeText = string.Empty;
            DetailSections = [];
            Description = null;
            HasDescription = false;
            FeaturesList = [];
            HasFeatures = false;
            return;
        }

        // House type: Show living area, rooms, year built only for House or Foreclosure with building data
        IsHouseType = Property.Type == PropertyType.House ||
                      (Property.Type == PropertyType.Foreclosure && (Property.LivingAreaM2.HasValue || Property.Rooms.HasValue));

        // Type badge text
        TypeBadgeText = Property.Type switch
        {
            PropertyType.House => "HAUS",
            PropertyType.Land => "GRUND",
            PropertyType.Foreclosure => "ZV",
            _ => "IMM"
        };

        // Type badge color (matches PropertyCard.xaml.cs logic)
        TypeBadgeBrush = Property.Type switch
        {
            PropertyType.House => GetThemeBrush("HausBrush"),
            PropertyType.Land => GetThemeBrush("GrundstueckBrush"),
            PropertyType.Foreclosure => GetThemeBrush("ZwangsversteigerungBrush"),
            _ => GetThemeBrush("AccentBrush")
        };

        // Format price: "3.590.000 €"
        FormattedPrice = $"{Property.Price:N0} €".Replace(",", ".");

        // Price per square meter
        if (Property.LivingAreaM2.HasValue && Property.LivingAreaM2.Value > 0)
        {
            var pricePerSqm = Property.Price / Property.LivingAreaM2.Value;
            PricePerSqmText = $"{pricePerSqm:N2} €".Replace(",", ".");
        }
        else
        {
            PricePerSqmText = "-";
        }

        // Plot area
        PlotAreaText = Property.PlotAreaM2.HasValue
            ? $"{Property.PlotAreaM2:N0} m²".Replace(",", ".")
            : "-";

        // Year built
        YearBuiltText = Property.YearBuilt?.ToString() ?? "-";

        // Full address: "Strasse, PLZ Ort"
        AddressText = $"{Property.Address}, {Property.PostalCode} {Property.City}";

        // Broker vs Private
        IsBroker = Property.SellerType == SellerType.Broker;
        OriginalListingUrl = Property.Contacts?.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.OriginalListingUrl))?.OriginalListingUrl;

        // Contact person (first contact name if available)
        var firstContact = Property.Contacts?.FirstOrDefault();
        HasContactPerson = firstContact != null;
        ContactPersonText = firstContact != null ? $"Herr/Frau {firstContact.Name}" : string.Empty;

        // Primary contact details for footer bar
        PrimaryContactEmail = firstContact?.Email;
        PrimaryContactPhone = firstContact?.Phone;
        HasPrimaryEmail = !string.IsNullOrWhiteSpace(PrimaryContactEmail);
        HasPrimaryPhone = !string.IsNullOrWhiteSpace(PrimaryContactPhone);

        // Description
        Description = Property.Description;
        HasDescription = !string.IsNullOrWhiteSpace(Property.Description);

        // Features
        FeaturesList = Property.Features ?? [];
        HasFeatures = FeaturesList.Count > 0;

        // Build structured data table
        BuildDetailSections();

        // Notify all computed properties
        OnPropertyChanged(nameof(HasContacts));
    }

    private void BuildDetailSections()
    {
        if (Property == null) return;

        var items = new List<PropertyDetailItem>();

        // --- Basisdaten (highlighted) ---
        var typeLabel = Property.Type switch
        {
            PropertyType.House => "Haus",
            PropertyType.Land => "Grundstueck",
            PropertyType.Foreclosure => "Zwangsversteigerung",
            _ => Property.Type.ToString()
        };
        AddIfNotEmpty(items, "Titel", Property.Title, PropertyDataCategory.Basisdaten, true);
        items.Add(new PropertyDetailItem("Immobilienart", typeLabel, PropertyDataCategory.Basisdaten, true));
        items.Add(new PropertyDetailItem("Kaufpreis", FormattedPrice, PropertyDataCategory.Basisdaten, true));
        AddIfNotEmpty(items, "PLZ", Property.PostalCode, PropertyDataCategory.Basisdaten, true);
        AddIfNotEmpty(items, "Ort", Property.City, PropertyDataCategory.Basisdaten, true);
        AddIfNotEmpty(items, "Adresse", Property.Address, PropertyDataCategory.Basisdaten, true);

        // Deserialize TypeSpecificData
        HousePropertyData? houseData = null;
        LandPropertyData? landData = null;
        ForeclosurePropertyData? foreclosureData = null;

        if (!string.IsNullOrWhiteSpace(Property.TypeSpecificData))
        {
            try
            {
                switch (Property.Type)
                {
                    case PropertyType.House:
                        houseData = JsonSerializer.Deserialize<HousePropertyData>(Property.TypeSpecificData, JsonOptions);
                        break;
                    case PropertyType.Land:
                        landData = JsonSerializer.Deserialize<LandPropertyData>(Property.TypeSpecificData, JsonOptions);
                        break;
                    case PropertyType.Foreclosure:
                        foreclosureData = JsonSerializer.Deserialize<ForeclosurePropertyData>(Property.TypeSpecificData, JsonOptions);
                        break;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "[PropertyDetail] Failed to deserialize TypeSpecificData");
            }
        }

        // --- Flaechen ---
        // Use TypeSpecificData if available, otherwise fall back to core fields
        if (houseData != null && houseData.LivingAreaInSquareMeters > 0)
            items.Add(new PropertyDetailItem("Wohnflaeche", FormatArea(houseData.LivingAreaInSquareMeters), PropertyDataCategory.Flaechen));
        else
            AddIfHasValue(items, "Wohnflaeche", Property.LivingAreaM2, v => $"{v:N0} m\u00B2".Replace(",", "."), PropertyDataCategory.Flaechen);

        if (landData != null && landData.PlotSizeInSquareMeters > 0)
            items.Add(new PropertyDetailItem("Grundstuecksflaeche", FormatArea(landData.PlotSizeInSquareMeters), PropertyDataCategory.Flaechen));
        else
            AddIfHasValue(items, "Grundstuecksflaeche", Property.PlotAreaM2, v => $"{v:N0} m\u00B2".Replace(",", "."), PropertyDataCategory.Flaechen);

        // --- Gebaeude (House) ---
        if (houseData != null)
        {
            if (houseData.TotalRooms > 0)
                items.Add(new PropertyDetailItem("Zimmer", houseData.TotalRooms.ToString(), PropertyDataCategory.Gebaeude));
            else
                AddIfHasValue(items, "Zimmer", Property.Rooms, v => v.ToString(), PropertyDataCategory.Gebaeude);

            if (houseData.Bedrooms > 0)
                items.Add(new PropertyDetailItem("Schlafzimmer", houseData.Bedrooms.ToString(), PropertyDataCategory.Gebaeude));
            if (houseData.Bathrooms > 0)
                items.Add(new PropertyDetailItem("Badezimmer", houseData.Bathrooms.ToString(), PropertyDataCategory.Gebaeude));
            if (houseData.Floors > 0)
                items.Add(new PropertyDetailItem("Stockwerke", houseData.Floors.ToString(), PropertyDataCategory.Gebaeude));
            if (houseData.YearBuilt.HasValue)
                items.Add(new PropertyDetailItem("Baujahr", houseData.YearBuilt.Value.ToString(), PropertyDataCategory.Gebaeude));
            else
                AddIfHasValue(items, "Baujahr", Property.YearBuilt, v => v.ToString(), PropertyDataCategory.Gebaeude);

            items.Add(new PropertyDetailItem("Zustand", FormatCondition(houseData.Condition), PropertyDataCategory.Gebaeude));

            if (houseData.ApartmentFloor.HasValue)
                items.Add(new PropertyDetailItem("Etage", houseData.ApartmentFloor.Value.ToString(), PropertyDataCategory.Gebaeude));
        }
        else
        {
            // Fallback to core fields when no HousePropertyData
            AddIfHasValue(items, "Zimmer", Property.Rooms, v => v.ToString(), PropertyDataCategory.Gebaeude);
            AddIfHasValue(items, "Baujahr", Property.YearBuilt, v => v.ToString(), PropertyDataCategory.Gebaeude);
        }

        // --- Ausstattung (House) ---
        if (houseData != null)
        {
            if (houseData.HasGarage)
                items.Add(new PropertyDetailItem("Garage", "Ja", PropertyDataCategory.Ausstattung));
            if (houseData.HasGarden)
                items.Add(new PropertyDetailItem("Garten", "Ja", PropertyDataCategory.Ausstattung));
            if (houseData.HasBasement)
                items.Add(new PropertyDetailItem("Keller", "Ja", PropertyDataCategory.Ausstattung));
            if (houseData.HasElevator == true)
                items.Add(new PropertyDetailItem("Aufzug", "Ja", PropertyDataCategory.Ausstattung));
        }

        // --- Grundstueck (Land) ---
        if (landData != null)
        {
            items.Add(new PropertyDetailItem("Widmung", FormatZoning(landData.Zoning), PropertyDataCategory.Grundstueck));
            items.Add(new PropertyDetailItem("Baurecht", FormatBool(landData.HasBuildingRights), PropertyDataCategory.Grundstueck));
            items.Add(new PropertyDetailItem("Bebaubar", FormatBool(landData.IsBuildable), PropertyDataCategory.Grundstueck));
            items.Add(new PropertyDetailItem("Versorgung", FormatBool(landData.HasUtilities), PropertyDataCategory.Grundstueck));
            if (landData.SoilQuality.HasValue)
                items.Add(new PropertyDetailItem("Bodenqualitaet", FormatSoilQuality(landData.SoilQuality.Value), PropertyDataCategory.Grundstueck));
        }

        // --- Versteigerung (Foreclosure) ---
        if (foreclosureData != null)
        {
            AddIfNotEmpty(items, "Gericht", foreclosureData.CourtName, PropertyDataCategory.Versteigerung);
            AddIfNotEmpty(items, "Aktenzeichen", foreclosureData.FileNumber, PropertyDataCategory.Versteigerung);
            items.Add(new PropertyDetailItem("Termin", foreclosureData.AuctionDate.ToString("dd.MM.yyyy"), PropertyDataCategory.Versteigerung));
            items.Add(new PropertyDetailItem("Mindestgebot", $"{foreclosureData.MinimumBid:N0} \u20AC".Replace(",", "."), PropertyDataCategory.Versteigerung));
            items.Add(new PropertyDetailItem("Status", FormatLegalStatus(foreclosureData.Status), PropertyDataCategory.Versteigerung));
        }

        // --- Kosten ---
        if (Property.LivingAreaM2.HasValue && Property.LivingAreaM2.Value > 0)
        {
            var pricePerSqm = Property.Price / Property.LivingAreaM2.Value;
            items.Add(new PropertyDetailItem("Preis / m\u00B2", $"{pricePerSqm:N2} \u20AC".Replace(",", "."), PropertyDataCategory.Kosten));
        }

        // --- Basisdaten: Eingestellt am ---
        items.Add(new PropertyDetailItem("Eingestellt am", Property.CreatedAt.ToString("dd.MM.yyyy"), PropertyDataCategory.Basisdaten));

        // Anbieter wird im Footer-Bar angezeigt, nicht in der Tabelle

        // Group by category, filter empty sections
        DetailSections = items
            .GroupBy(i => i.Category)
            .OrderBy(g => g.Key)
            .Select(g => new PropertyDetailSection(GetCategoryTitle(g.Key), g.Key, g.ToList()))
            .ToList();
    }

    #region Formatting Helpers

    private static string FormatBool(bool value) => value ? "Ja" : "Nein";

    private static string FormatArea(decimal sqm) => $"{sqm:N0} m\u00B2".Replace(",", ".");

    private static string FormatCondition(PropertyCondition condition) => condition switch
    {
        PropertyCondition.LikeNew => "Neuwertig",
        PropertyCondition.Good => "Gut",
        PropertyCondition.Average => "Durchschnittlich",
        PropertyCondition.NeedsRenovation => "Sanierungsbeduerftig",
        _ => condition.ToString()
    };

    private static string FormatZoning(ZoningType zoning) => zoning switch
    {
        ZoningType.Residential => "Wohngebiet",
        ZoningType.Commercial => "Gewerbegebiet",
        ZoningType.Industrial => "Industriegebiet",
        ZoningType.Agricultural => "Landwirtschaft",
        ZoningType.Mixed => "Mischgebiet",
        _ => zoning.ToString()
    };

    private static string FormatSoilQuality(SoilQuality quality) => quality switch
    {
        SoilQuality.High => "Hoch",
        SoilQuality.Medium => "Mittel",
        SoilQuality.Low => "Niedrig",
        _ => quality.ToString()
    };

    private static string FormatLegalStatus(LegalStatus status) => status switch
    {
        LegalStatus.Pending => "Anhaengig",
        LegalStatus.Scheduled => "Terminiert",
        LegalStatus.InProgress => "Laufend",
        LegalStatus.Completed => "Abgeschlossen",
        LegalStatus.Cancelled => "Aufgehoben",
        _ => status.ToString()
    };

    private static string GetCategoryTitle(PropertyDataCategory category) => category switch
    {
        PropertyDataCategory.Basisdaten => "BASISDATEN",
        PropertyDataCategory.Flaechen => "FLAECHEN",
        PropertyDataCategory.Gebaeude => "GEBAEUDE",
        PropertyDataCategory.Ausstattung => "AUSSTATTUNG",
        PropertyDataCategory.Grundstueck => "GRUNDSTUECK",
        PropertyDataCategory.Versteigerung => "VERSTEIGERUNG",
        PropertyDataCategory.Kosten => "KOSTEN",
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
            BusyMessage = "Lade Immobilie...";
        });

        try
        {
            _logger.LogInformation("[PropertyDetail] Loading property {PropertyId} from API", propertyId);

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

                _logger.LogInformation("[PropertyDetail] Property loaded: {Title}", loadedProperty.Title);
            }
            else
            {
                _logger.LogWarning("[PropertyDetail] Property {PropertyId} not found", propertyId);
            }

            // Load favorite status (can run on any thread)
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
            _logger.LogError(ex, "[PropertyDetail] Error loading property {PropertyId}", propertyId);
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

        // Build share URL for the property
        var propertyUrl = new Uri($"https://heimatplatz.at/immobilie/{Property.Id}");

        var description = $"{Property.Title}\n" +
                          $"{FormattedPrice}\n" +
                          $"{AddressText}";

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
    /// Oeffnet die Original-Anbieterseite im Browser
    /// </summary>
    [RelayCommand]
    private async Task OpenOriginalListingAsync()
    {
        if (string.IsNullOrWhiteSpace(OriginalListingUrl))
            return;

        _logger.LogInformation("[PropertyDetail] Opening original listing: {Url}", OriginalListingUrl);

        await Windows.System.Launcher.LaunchUriAsync(new Uri(OriginalListingUrl));
    }

    #region INavigationAware Implementation

    /// <inheritdoc />
    public void OnNavigatedTo(object? parameter)
    {
        _logger.LogDebug("[PropertyDetail] OnNavigatedTo");
        _dispatcher ??= DispatcherQueue.GetForCurrentThread();
        _ = LoadPropertyAsync(_propertyId);
    }

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
        _logger.LogDebug("[PropertyDetail] OnNavigatedFrom");
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
        // Fallback to a default brush
        return new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray);
    }
}
