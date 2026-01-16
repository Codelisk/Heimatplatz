# Heimatplatz.Api.Features.ForeclosureAuctions.Contracts

Contracts (DTOs und Interfaces) für das ForeclosureAuctions-Feature.

## Zweck

Definiert die öffentlichen Schnittstellen und Datenstrukturen für Zwangsversteigerungen, die zwischen verschiedenen Schichten der Anwendung ausgetauscht werden.

## Enthaltene Komponenten

### Mediator Requests/Responses

- `GetForeclosureAuctionsRequest` / `GetForeclosureAuctionsResponse` - Abrufen aller Versteigerungen mit optionalen Filtern
- `GetForeclosureAuctionByIdRequest` / `GetForeclosureAuctionByIdResponse` - Abrufen einer einzelnen Versteigerung

### DTOs

- `ForeclosureAuctionDto` - Vollständige Zwangsversteigerungs-Details

### Enums

- `AustrianState` - Österreichische Bundesländer
- `PropertyCategory` - Kategorien von Liegenschaften

## Filter-Optionen

`GetForeclosureAuctionsRequest` unterstützt folgende Filter:

- `State`: Nach Bundesland filtern
- `Category`: Nach Immobilienkategorie filtern
- `City`: Nach Ort filtern
- `PostalCode`: Nach Postleitzahl filtern
- `AuctionDateFrom` / `AuctionDateTo`: Nach Versteigerungsdatum filtern
- `MaxEstimatedValue`: Nach maximalem geschätzten Wert filtern

## Abhängigkeiten

- `Shiny.Mediator.Contracts`

## Verwendung

```csharp
// Beispiel: Alle Versteigerungen in Wien abrufen
var request = new GetForeclosureAuctionsRequest
{
    State = AustrianState.Wien
};

var response = await mediator.Send(request);

foreach (var auction in response.Auctions)
{
    Console.WriteLine($"{auction.ObjectDescription} - {auction.AuctionDate:d}");
}
```
