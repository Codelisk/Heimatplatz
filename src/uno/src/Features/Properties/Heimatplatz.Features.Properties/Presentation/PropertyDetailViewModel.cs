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
    [NotifyPropertyChangedFor(nameof(FavoriteButtonText))]
    [NotifyPropertyChangedFor(nameof(FavoriteButtonIcon))]
    private bool _isFavorite;

    [ObservableProperty]
    private bool _isHouseType;

    [ObservableProperty]
    private string _typeBadgeText = string.Empty;

    [ObservableProperty]
    private Microsoft.UI.Xaml.Media.Brush? _typeBadgeBrush;

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

        // Property data is loaded in OnNavigatedTo (guaranteed UI thread for dispatcher access)
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

        // Contact person (first contact name if available)
        var firstContact = Property.Contacts?.FirstOrDefault();
        HasContactPerson = firstContact != null;
        ContactPersonText = firstContact != null ? $"Herr/Frau {firstContact.Name}" : string.Empty;

        // Notify all computed properties
        OnPropertyChanged(nameof(HasContacts));
    }

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
