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
    private string _ausstattungText = "Keine Angaben";

    private void UpdateDisplayProperties()
    {
        if (Property == null)
        {
            FormattedPrice = string.Empty;
            DetailsText = string.Empty;
            TypeText = string.Empty;
            AusstattungText = "Keine Angaben";
            return;
        }

        // Preis formatieren
        FormattedPrice = $"€ {Property.Preis:N0}".Replace(",", ".");

        // Details zusammenstellen
        var parts = new List<string>();
        if (Property.WohnflaecheM2.HasValue)
            parts.Add($"{Property.WohnflaecheM2} m²");
        if (Property.Zimmer.HasValue)
            parts.Add($"{Property.Zimmer} Zimmer");
        if (Property.Baujahr.HasValue)
            parts.Add($"Baujahr {Property.Baujahr}");
        DetailsText = string.Join(" · ", parts);

        // Typ-Text
        TypeText = Property.Typ switch
        {
            PropertyType.Haus => "HAUS",
            PropertyType.Grundstueck => "GRUNDSTÜCK",
            PropertyType.Zwangsversteigerung => "ZWANGSVERSTEIGERUNG",
            _ => string.Empty
        };

        // Ausstattung
        AusstattungText = Property.Ausstattung?.Count > 0
            ? string.Join("  ", Property.Ausstattung.Select(a => $"✓ {a}"))
            : "Keine Angaben";
    }

    public void LoadProperty(Guid propertyId)
    {
        IsBusy = true;
        BusyMessage = "Lade Immobilie...";

        try
        {
            // TODO: API-Integration - vorerst Testdaten
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
        // TODO: Kontaktformular oder E-Mail oeffnen
        System.Diagnostics.Debug.WriteLine($"Kontaktiere Anbieter: {Property?.AnbieterName}");
    }

    private PropertyDetailDto GetMockProperty(Guid propertyId)
    {
        // Mock-Daten basierend auf den Testdaten in HomeViewModel
        return new PropertyDetailDto(
            Id: propertyId,
            Titel: "Einfamilienhaus in Linz-Urfahr",
            Adresse: "Hauptstrasse 15",
            Ort: "Linz",
            Preis: 349000,
            WohnflaecheM2: 145,
            GrundstuecksflaecheM2: 520,
            Zimmer: 5,
            Baujahr: 2018,
            Typ: PropertyType.Haus,
            AnbieterTyp: SellerType.Makler,
            AnbieterName: "Mustermann Immobilien",
            AnbieterTelefon: "+43 732 123456",
            AnbieterEmail: "info@mustermann-immo.at",
            BildUrls:
            [
                "https://picsum.photos/seed/haus1a/800/600",
                "https://picsum.photos/seed/haus1b/800/600",
                "https://picsum.photos/seed/haus1c/800/600"
            ],
            Beschreibung: "Wunderschönes Einfamilienhaus mit großem Garten in ruhiger Lage. " +
                          "Das Haus wurde 2018 erbaut und befindet sich in einem ausgezeichneten Zustand. " +
                          "Die hochwertige Ausstattung und die durchdachte Raumaufteilung machen dieses Objekt " +
                          "zu einem idealen Zuhause für Familien.",
            Ausstattung: ["Garage", "Garten", "Terrasse", "Keller", "Fußbodenheizung", "Photovoltaik"]
        );
    }
}
