using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UnoFramework.Contracts.Navigation;
using UnoFramework.Contracts.Pages;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Models;
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
    private readonly IMediator _mediator;
    private readonly IAuthService _authService;
    private readonly IPropertyStatusService _propertyStatusService;
    private readonly ILogger<PropertyDetailViewModel> _logger;
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
    [NotifyPropertyChangedFor(nameof(FavoriteButtonText))]
    [NotifyPropertyChangedFor(nameof(FavoriteButtonIcon))]
    private bool _isFavorite;

    [ObservableProperty]
    private bool _isHouseType;

    [ObservableProperty]
    private string _typeBadgeText = string.Empty;

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
        IMediator mediator,
        IAuthService authService,
        IPropertyStatusService propertyStatusService,
        ILogger<PropertyDetailViewModel> logger,
        PropertyDetailData data)
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

    /// <summary>
    /// Gibt an, ob Kontaktdaten verfuegbar sind
    /// </summary>
    public bool HasContacts => Property?.Contacts?.Count > 0;

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
            TypeBadgeText = string.Empty;
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

        // Contact person (first contact name if available)
        var firstContact = Property.Contacts?.FirstOrDefault();
        HasContactPerson = firstContact != null;
        ContactPersonText = firstContact != null ? $"Herr/Frau {firstContact.Name}" : string.Empty;

        // Notify all computed properties
        OnPropertyChanged(nameof(HasContacts));
    }

    public async Task LoadPropertyAsync(Guid propertyId)
    {
        IsBusy = true;
        BusyMessage = "Lade Immobilie...";

        try
        {
            _logger.LogInformation("[PropertyDetail] Loading property {PropertyId} from API", propertyId);

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

                _logger.LogInformation("[PropertyDetail] Property loaded: {Title}", Property.Title);
            }
            else
            {
                _logger.LogWarning("[PropertyDetail] Property {PropertyId} not found", propertyId);
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
            _logger.LogError(ex, "[PropertyDetail] Error loading property {PropertyId}", propertyId);
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

        _logger.LogInformation("[PropertyDetail] Toggling favorite for {PropertyId}", Property.Id);

        IsFavorite = await _propertyStatusService.ToggleFavoriteAsync(Property.Id);
    }

    #region INavigationAware Implementation

    /// <inheritdoc />
    public void OnNavigatedTo(object? parameter)
    {
        _logger.LogDebug("[PropertyDetail] OnNavigatedTo");
    }

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
        _logger.LogDebug("[PropertyDetail] OnNavigatedFrom");
    }

    #endregion
}
