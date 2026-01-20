using CommunityToolkit.Mvvm.ComponentModel;
using Heimatplatz.Features.Properties.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Models;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// ViewModel fuer die PropertyDetailPage
/// </summary>
public partial class PropertyDetailViewModel : ObservableObject
{
    private readonly IClipboardService _clipboardService;

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

    public PropertyDetailViewModel(IClipboardService clipboardService)
    {
        _clipboardService = clipboardService;
    }

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

    public void LoadProperty(Guid propertyId)
    {
        IsBusy = true;
        BusyMessage = "Lade Immobilie...";

        try
        {
            // TODO: API integration - mock data for now
            Property = GetMockProperty(propertyId);
            UpdateDisplayProperties();
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
