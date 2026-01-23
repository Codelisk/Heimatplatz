using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UnoFramework.Contracts.Pages;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Models;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;
using UnoFramework.Contracts.Navigation;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// ViewModel fuer die PropertyDetailPage
/// Implements IPageInfo for header integration (shows back button)
/// </summary>
[Service(UnoService.Lifetime, TryAdd = UnoService.TryAdd)]
public partial class PropertyDetailViewModel : ObservableObject, INavigationAware, IPageInfo
{
    private readonly IClipboardService _clipboardService;
    private readonly IMediator _mediator;
    private readonly IAuthService _authService;
    private readonly IPropertyStatusService _propertyStatusService;
    private readonly ILogger<PropertyDetailViewModel> _logger;

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
    private string _detailsText = string.Empty;

    [ObservableProperty]
    private string _typeText = string.Empty;

    [ObservableProperty]
    private string _featuresText = "Keine Angaben";

    [ObservableProperty]
    private string? _copyFeedback;

    [ObservableProperty]
    private string? _statusFeedback;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FavoriteButtonText))]
    [NotifyPropertyChangedFor(nameof(FavoriteButtonIcon))]
    private bool _isFavorite;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BlockButtonText))]
    private bool _isBlocked;

    /// <summary>
    /// Text for the favorite button based on current status
    /// </summary>
    public string FavoriteButtonText => IsFavorite ? "Entfavorisieren" : "Favorisieren";

    /// <summary>
    /// Icon glyph for the favorite button (filled/outline heart)
    /// </summary>
    public string FavoriteButtonIcon => IsFavorite ? "\uEB52" : "\uEB51";

    /// <summary>
    /// Text for the block button based on current status
    /// </summary>
    public string BlockButtonText => IsBlocked ? "Entblockieren" : "Blockieren";

    #region IPageInfo Implementation

    public PageType PageType => PageType.Detail;
    public string PageTitle => Property?.Title ?? "Immobilie";
    public Type? MainHeaderViewModel => null;

    #endregion

    /// <summary>
    /// Event that is raised when the property has been blocked and navigation should occur
    /// </summary>
    public event EventHandler? PropertyBlocked;

    public PropertyDetailViewModel(
        IClipboardService clipboardService,
        IMediator mediator,
        IAuthService authService,
        IPropertyStatusService propertyStatusService,
        ILogger<PropertyDetailViewModel> logger)
    {
        _clipboardService = clipboardService;
        _mediator = mediator;
        _authService = authService;
        _propertyStatusService = propertyStatusService;
        _logger = logger;
    }

    /// <summary>
    /// Indicates if the user can block properties (must be authenticated)
    /// </summary>
    public bool CanBlock => _authService.IsAuthenticated;

    /// <summary>
    /// Gibt an, ob Kontaktdaten verfuegbar sind
    /// </summary>
    public bool HasContacts => Property?.Contacts?.Count > 0;

    /// <summary>
    /// Gibt an, ob der Kontakt-Button angezeigt werden soll
    /// </summary>
    public bool ShowContactButton => Property?.InquiryType == InquiryType.ContactData && HasContacts;

    private void UpdateDisplayProperties()
    {
        if (Property == null)
        {
            FormattedPrice = string.Empty;
            DetailsText = string.Empty;
            TypeText = string.Empty;
            FeaturesText = "Keine Angaben";
            return;
        }

        // Format price
        FormattedPrice = $"€ {Property.Price:N0}".Replace(",", ".");

        // Build details text
        var parts = new List<string>();
        if (Property.LivingAreaM2.HasValue)
            parts.Add($"{Property.LivingAreaM2} m²");
        if (Property.Rooms.HasValue)
            parts.Add($"{Property.Rooms} Zimmer");
        if (Property.YearBuilt.HasValue)
            parts.Add($"Baujahr {Property.YearBuilt}");
        DetailsText = string.Join(" · ", parts);

        // Type text
        TypeText = Property.Type switch
        {
            PropertyType.House => "HAUS",
            PropertyType.Land => "GRUNDSTÜCK",
            PropertyType.Foreclosure => "ZWANGSVERSTEIGERUNG",
            _ => string.Empty
        };

        // Features
        FeaturesText = Property.Features?.Count > 0
            ? string.Join("  ", Property.Features.Select(a => $"✓ {a}"))
            : "Keine Angaben";

        OnPropertyChanged(nameof(HasContacts));
        OnPropertyChanged(nameof(ShowContactButton));
    }

    public async Task LoadPropertyAsync(Guid propertyId)
    {
        IsBusy = true;
        BusyMessage = "Lade Immobilie...";

        try
        {
            // TODO: API integration - mock data for now
            Property = GetMockProperty(propertyId);
            UpdateDisplayProperties();

            // Load favorite and blocked status
            if (_authService.IsAuthenticated)
            {
                await _propertyStatusService.RefreshStatusAsync();
                IsFavorite = _propertyStatusService.IsFavorite(propertyId);
                IsBlocked = _propertyStatusService.IsBlocked(propertyId);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Keep synchronous version for backward compatibility
    public void LoadProperty(Guid propertyId)
    {
        _ = LoadPropertyAsync(propertyId);
    }

    public void OnNavigatedTo(object? parameter)
    {
        // Parameter kann Guid oder string sein
        Guid? propertyId = parameter switch
        {
            Guid g => g,
            string s when Guid.TryParse(s, out var parsed) => parsed,
            _ => null
        };

        if (propertyId.HasValue)
        {
            LoadProperty(propertyId.Value);
        }
        else
        {
            // Fallback: Lade Testdaten
            LoadProperty(Guid.NewGuid());
        }
    }

    public void OnNavigatedFrom()
    {
        // Cleanup if needed
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
        if (Property == null || !CanBlock)
            return;

        _logger.LogInformation("[PropertyDetail] Toggling favorite for {PropertyId}", Property.Id);

        var wasFavorite = IsFavorite;
        IsFavorite = await _propertyStatusService.ToggleFavoriteAsync(Property.Id);

        // Show feedback
        StatusFeedback = IsFavorite ? "Zu Favoriten hinzugefuegt" : "Aus Favoriten entfernt";
        await Task.Delay(1500);
        StatusFeedback = null;
    }

    /// <summary>
    /// Toggles the blocked status of the current property
    /// </summary>
    [RelayCommand]
    private async Task ToggleBlockAsync()
    {
        if (Property == null || !CanBlock)
            return;

        _logger.LogInformation("[PropertyDetail] Toggling block for {PropertyId}", Property.Id);

        var wasBlocked = IsBlocked;
        IsBlocked = await _propertyStatusService.ToggleBlockedAsync(Property.Id);

        if (IsBlocked && !wasBlocked)
        {
            // Property was blocked - also remove from favorites if it was favorited
            if (IsFavorite)
            {
                IsFavorite = false;
            }

            StatusFeedback = "Blockiert";

            // Notify that the property was blocked - page should navigate back
            PropertyBlocked?.Invoke(this, EventArgs.Empty);
        }
        else if (!IsBlocked && wasBlocked)
        {
            // Property was unblocked
            StatusFeedback = "Blockierung aufgehoben";
            await Task.Delay(1500);
            StatusFeedback = null;
        }
    }

    /// <summary>
    /// Gibt den Anzeigenamen fuer einen ContactType zurueck
    /// </summary>
    public static string GetContactTypeName(ContactType type) => type switch
    {
        ContactType.Seller => "Eigentümer",
        ContactType.Agent => "Makler",
        ContactType.PropertyManager => "Hausverwaltung",
        _ => "Kontakt"
    };

    private PropertyDetailDto GetMockProperty(Guid propertyId)
    {
        // Mock data with new Contacts structure
        return new PropertyDetailDto(
            Id: propertyId,
            Title: "Einfamilienhaus in Linz-Urfahr",
            Address: "Hauptstrasse 15",
            City: "Linz",
            Price: 349000,
            LivingAreaM2: 145,
            PlotAreaM2: 520,
            Rooms: 5,
            YearBuilt: 2018,
            Type: PropertyType.House,
            SellerType: SellerType.Makler,
            SellerName: "Mustermann Immobilien",
            ImageUrls:
            [
                "https://picsum.photos/seed/haus1a/800/600",
                "https://picsum.photos/seed/haus1b/800/600",
                "https://picsum.photos/seed/haus1c/800/600"
            ],
            Description: "Wunderschönes Einfamilienhaus mit großem Garten in ruhiger Lage. " +
                         "Das Haus wurde 2018 erbaut und befindet sich in einem ausgezeichneten Zustand. " +
                         "Die hochwertige Ausstattung und die durchdachte Raumaufteilung machen dieses Objekt " +
                         "zu einem idealen Zuhause für Familien.",
            Features: ["Garage", "Garten", "Terrasse", "Keller", "Fußbodenheizung", "Photovoltaik"],
            InquiryType: InquiryType.ContactData,
            Contacts:
            [
                new ContactInfoDto(
                    Id: Guid.NewGuid(),
                    Type: ContactType.Agent,
                    Source: ContactSource.Manual,
                    Name: "Mustermann Immobilien",
                    Email: "info@mustermann-immo.at",
                    Phone: "+43 732 123456",
                    OriginalListingUrl: null,
                    SourceName: null,
                    DisplayOrder: 0
                ),
                new ContactInfoDto(
                    Id: Guid.NewGuid(),
                    Type: ContactType.Seller,
                    Source: ContactSource.Manual,
                    Name: "Herr Huber",
                    Email: null,
                    Phone: "+43 664 9876543",
                    OriginalListingUrl: null,
                    SourceName: null,
                    DisplayOrder: 1
                )
            ]
        );
    }
}
