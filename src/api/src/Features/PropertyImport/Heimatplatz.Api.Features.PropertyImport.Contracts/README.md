# Heimatplatz.Api.Features.PropertyImport.Contracts

## Zweck

Dieses Projekt enthaelt die Contracts (DTOs, Requests, Responses) fuer das PropertyImport Feature.

## Hauptkomponenten

### Mediator Requests

- **ImportPropertiesRequest** - Batch-Import Request fuer Properties
- **ImportPropertiesResponse** - Response mit Import-Statistiken
- **ImportPropertyDto** - DTO fuer eine einzelne Property beim Import
- **ImportContactDto** - DTO fuer Kontaktinformationen beim Import
- **ImportResultItem** - Ergebnis fuer eine einzelne Property
- **ImportResultStatus** - Status-Enum (Created, Updated, Skipped, Failed)

## Abhaengigkeiten

- `Heimatplatz.Api.Features.Properties.Contracts` - PropertyType, SellerType, ContactType, InquiryType
- `Shiny.Mediator.Contracts` - IRequest Interface

## Verwendung

```csharp
var request = new ImportPropertiesRequest(
    Properties: [
        new ImportPropertyDto(
            SourceName: "Willhaben",
            SourceId: "12345",
            SourceUrl: "https://willhaben.at/...",
            Title: "Einfamilienhaus",
            Address: "Musterstrasse 1",
            City: "Wien",
            PostalCode: "1010",
            Price: 450000,
            Type: PropertyType.House,
            SellerType: SellerType.Portal,
            SellerName: "Willhaben"
        )
    ],
    UpdateExisting: true
);
```
