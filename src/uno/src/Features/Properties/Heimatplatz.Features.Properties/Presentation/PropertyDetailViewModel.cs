using CommunityToolkit.Mvvm.ComponentModel;
using Heimatplatz.Features.Properties.Contracts.Models;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// ViewModel fuer die PropertyDetailPage
/// </summary>
public partial class PropertyDetailViewModel : ObservableObject
{
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

    public void ContactSeller()
    {
        // TODO: Open contact form or email
        System.Diagnostics.Debug.WriteLine($"Contacting seller: {Property?.SellerName}");
    }

    private PropertyDetailDto GetMockProperty(Guid propertyId)
    {
        // Mock data based on test data in HomeViewModel
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
            SellerPhone: "+43 732 123456",
            SellerEmail: "info@mustermann-immo.at",
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
            Features: ["Garage", "Garten", "Terrasse", "Keller", "Fußbodenheizung", "Photovoltaik"]
        );
    }
}
