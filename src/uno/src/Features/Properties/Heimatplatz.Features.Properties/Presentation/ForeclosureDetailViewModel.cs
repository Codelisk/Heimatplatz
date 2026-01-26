using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UnoFramework.Contracts.Navigation;
using UnoFramework.Contracts.Pages;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Models;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;
using System.Text.Json;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// ViewModel fuer die ForeclosureDetailPage (Zwangsversteigerungen)
/// </summary>
[Service(UnoService.Lifetime, TryAdd = UnoService.TryAdd)]
public partial class ForeclosureDetailViewModel : ObservableObject, IPageInfo, INavigationAware
{
    private readonly IClipboardService _clipboardService;
    private readonly IMediator _mediator;
    private readonly IAuthService _authService;
    private readonly IPropertyStatusService _propertyStatusService;
    private readonly ILogger<ForeclosureDetailViewModel> _logger;
    private readonly Guid _propertyId;

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
    private string _totalAreaText = "-";

    [ObservableProperty]
    private string _plotAreaText = "-";

    [ObservableProperty]
    private string _buildingAreaText = "-";

    [ObservableProperty]
    private string _addressText = string.Empty;

    [ObservableProperty]
    private string? _copyFeedback;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FavoriteButtonText))]
    [NotifyPropertyChangedFor(nameof(FavoriteButtonIcon))]
    private bool _isFavorite;

    // === Foreclosure-specific properties ===

    [ObservableProperty]
    private string _auctionDateText = "-";

    [ObservableProperty]
    private string _estimatedValueText = "-";

    [ObservableProperty]
    private string _minimumBidText = "-";

    [ObservableProperty]
    private string _courtText = "-";

    [ObservableProperty]
    private string _caseNumberText = "-";

    [ObservableProperty]
    private string _categoryText = "-";

    [ObservableProperty]
    private string _statusText = "-";

    [ObservableProperty]
    private string _registrationNumberText = "-";

    [ObservableProperty]
    private string _cadastralMunicipalityText = "-";

    [ObservableProperty]
    private string _plotNumberText = "-";

    [ObservableProperty]
    private string _zoningDesignationText = "-";

    [ObservableProperty]
    private string _buildingConditionText = "-";

    [ObservableProperty]
    private string _numberOfRoomsText = "-";

    [ObservableProperty]
    private string _yearBuiltText = "-";

    [ObservableProperty]
    private string _viewingDateText = "-";

    [ObservableProperty]
    private string _biddingDeadlineText = "-";

    [ObservableProperty]
    private string _ownershipShareText = "-";

    [ObservableProperty]
    private string _notesText = "-";

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
    private bool _hasRoomInfo;

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
        IMediator mediator,
        IAuthService authService,
        IPropertyStatusService propertyStatusService,
        ILogger<ForeclosureDetailViewModel> logger,
        ForeclosureDetailData data)
    {
        _clipboardService = clipboardService;
        _mediator = mediator;
        _authService = authService;
        _propertyStatusService = propertyStatusService;
        _logger = logger;
        _propertyId = data.PropertyId;

        // Load property data immediately
        _ = LoadPropertyAsync(_propertyId);
    }

    private void UpdateDisplayProperties()
    {
        if (Property == null)
        {
            FormattedPrice = string.Empty;
            TotalAreaText = "-";
            PlotAreaText = "-";
            BuildingAreaText = "-";
            AddressText = string.Empty;
            return;
        }

        // Format price: "3.590.000 €"
        FormattedPrice = $"{Property.Price:N0} €".Replace(",", ".");

        // Plot area
        PlotAreaText = Property.PlotAreaM2.HasValue
            ? $"{Property.PlotAreaM2:N0} m²".Replace(",", ".")
            : "-";

        // Full address: "Strasse, PLZ Ort"
        AddressText = $"{Property.Address}, {Property.PostalCode} {Property.City}";

        // Parse TypeSpecificData for foreclosure-specific info
        ParseForeclosureData();
    }

    private void ParseForeclosureData()
    {
        if (string.IsNullOrEmpty(Property?.TypeSpecificData))
            return;

        try
        {
            var data = JsonSerializer.Deserialize<JsonElement>(Property.TypeSpecificData);

            // Auction date
            if (data.TryGetProperty("AuctionDate", out var auctionDate))
            {
                if (DateTime.TryParse(auctionDate.GetString(), out var dt))
                    AuctionDateText = dt.ToString("dd.MM.yyyy HH:mm");
            }

            // Estimated value
            if (data.TryGetProperty("EstimatedValue", out var estValue) && estValue.TryGetDecimal(out var ev))
                EstimatedValueText = $"{ev:N0} €".Replace(",", ".");

            // Minimum bid
            if (data.TryGetProperty("MinimumBid", out var minBid) && minBid.TryGetDecimal(out var mb))
                MinimumBidText = $"{mb:N0} €".Replace(",", ".");

            // Court (JSON property name: CourtName)
            if (data.TryGetProperty("CourtName", out var court))
                CourtText = court.GetString() ?? "-";

            // Case number (JSON property name: FileNumber)
            if (data.TryGetProperty("FileNumber", out var caseNum))
                CaseNumberText = caseNum.GetString() ?? "-";

            // Category
            if (data.TryGetProperty("Category", out var category))
                CategoryText = category.GetString() ?? "-";

            // Status - translate to German
            if (data.TryGetProperty("Status", out var status))
            {
                var statusValue = status.GetString() ?? "";
                StatusText = TranslateStatus(statusValue);
            }

            // Registration number (EZ)
            if (data.TryGetProperty("RegistrationNumber", out var regNum))
                RegistrationNumberText = regNum.GetString() ?? "-";

            // Cadastral municipality (KG)
            if (data.TryGetProperty("CadastralMunicipality", out var cadastral))
                CadastralMunicipalityText = cadastral.GetString() ?? "-";

            // Plot number
            if (data.TryGetProperty("PlotNumber", out var plotNum))
                PlotNumberText = plotNum.GetString() ?? "-";

            // Total area
            if (data.TryGetProperty("TotalArea", out var totalArea) && totalArea.TryGetDecimal(out var ta))
                TotalAreaText = $"{ta:N0} m²".Replace(",", ".");

            // Building area
            if (data.TryGetProperty("BuildingArea", out var buildingArea) && buildingArea.TryGetDecimal(out var ba))
                BuildingAreaText = $"{ba:N0} m²".Replace(",", ".");

            // Zoning designation
            if (data.TryGetProperty("ZoningDesignation", out var zoning))
                ZoningDesignationText = zoning.GetString() ?? "-";

            // Building condition
            if (data.TryGetProperty("BuildingCondition", out var condition))
                BuildingConditionText = condition.GetString() ?? "-";

            // Number of rooms
            if (data.TryGetProperty("NumberOfRooms", out var rooms) && rooms.TryGetInt32(out var r))
            {
                NumberOfRoomsText = r.ToString();
                HasRoomInfo = true;
            }

            // Year built
            if (data.TryGetProperty("YearBuilt", out var yearBuilt) && yearBuilt.TryGetInt32(out var yb))
            {
                YearBuiltText = yb.ToString();
                HasRoomInfo = true;
            }

            // Viewing date
            if (data.TryGetProperty("ViewingDate", out var viewingDate))
            {
                if (DateTime.TryParse(viewingDate.GetString(), out var vd))
                    ViewingDateText = vd.ToString("dd.MM.yyyy HH:mm");
            }

            // Bidding deadline
            if (data.TryGetProperty("BiddingDeadline", out var biddingDeadline))
            {
                if (DateTime.TryParse(biddingDeadline.GetString(), out var bd))
                    BiddingDeadlineText = bd.ToString("dd.MM.yyyy HH:mm");
            }

            // Ownership share
            if (data.TryGetProperty("OwnershipShare", out var ownership))
                OwnershipShareText = ownership.GetString() ?? "-";

            // Notes
            if (data.TryGetProperty("Notes", out var notes))
                NotesText = notes.GetString() ?? "-";

            // Documents
            if (data.TryGetProperty("EdictUrl", out var edictUrl))
                EdictUrl = edictUrl.GetString();

            if (data.TryGetProperty("FloorPlanUrl", out var floorPlan))
                FloorPlanUrl = floorPlan.GetString();

            if (data.TryGetProperty("SitePlanUrl", out var sitePlan))
                SitePlanUrl = sitePlan.GetString();

            if (data.TryGetProperty("LongAppraisalUrl", out var longAppraisal))
                LongAppraisalUrl = longAppraisal.GetString();

            if (data.TryGetProperty("ShortAppraisalUrl", out var shortAppraisal))
                ShortAppraisalUrl = shortAppraisal.GetString();

            HasDocuments = !string.IsNullOrEmpty(EdictUrl) ||
                           !string.IsNullOrEmpty(FloorPlanUrl) ||
                           !string.IsNullOrEmpty(SitePlanUrl) ||
                           !string.IsNullOrEmpty(LongAppraisalUrl) ||
                           !string.IsNullOrEmpty(ShortAppraisalUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ForeclosureDetail] Error parsing TypeSpecificData");
        }
    }

    /// <summary>
    /// Translates English status values to German
    /// </summary>
    private static string TranslateStatus(string status)
    {
        return status switch
        {
            "Pending" => "Anhängig",
            "Scheduled" => "Termin angesetzt",
            "InProgress" => "Laufend",
            "Completed" => "Abgeschlossen",
            "Cancelled" => "Abgebrochen",
            "Suspended" => "Ausgesetzt",
            _ => status
        };
    }

    public async Task LoadPropertyAsync(Guid propertyId)
    {
        IsBusy = true;
        BusyMessage = "Lade Zwangsversteigerung...";

        try
        {
            _logger.LogInformation("[ForeclosureDetail] Loading property {PropertyId} from API", propertyId);

            var request = new Heimatplatz.Core.ApiClient.Generated.GetPropertyByIdHttpRequest
            {
                Id = propertyId
            };

            var (context, response) = await _mediator.Request(request);

            if (response?.Property != null)
            {
                var prop = response.Property;
                Property = new PropertyDetailDto(
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

                _logger.LogInformation("[ForeclosureDetail] Property loaded: {Title}", Property.Title);
            }
            else
            {
                _logger.LogWarning("[ForeclosureDetail] Property {PropertyId} not found", propertyId);
            }

            UpdateDisplayProperties();

            // Load favorite status
            if (_authService.IsAuthenticated)
            {
                await _propertyStatusService.RefreshStatusAsync();
                IsFavorite = _propertyStatusService.IsFavorite(propertyId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ForeclosureDetail] Error loading property {PropertyId}", propertyId);
        }
        finally
        {
            IsBusy = false;
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

    #region INavigationAware Implementation

    /// <inheritdoc />
    public void OnNavigatedTo(object? parameter)
    {
        _logger.LogDebug("[ForeclosureDetail] OnNavigatedTo");
    }

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
        _logger.LogDebug("[ForeclosureDetail] OnNavigatedFrom");
    }

    #endregion
}
