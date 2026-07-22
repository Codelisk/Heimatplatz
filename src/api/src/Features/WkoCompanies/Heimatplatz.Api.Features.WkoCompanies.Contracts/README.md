# Heimatplatz.Api.Features.WkoCompanies.Contracts

Contracts (DTOs und Interfaces) für das WkoCompanies-Feature.

## Zweck

Definiert die öffentlichen Schnittstellen und Datenstrukturen für Firmen aus dem WKO
Firmen-A-Z-Verzeichnis (firmen.wko.at), die zwischen verschiedenen Schichten der
Anwendung ausgetauscht werden.

## Enthaltene Komponenten

### Mediator Requests/Responses

- `GetWkoCompaniesRequest` / `GetWkoCompaniesResponse` - Abrufen aller Firmen mit optionalen Filtern
- `GetWkoCompanyByIdRequest` / `GetWkoCompanyByIdResponse` - Abrufen einer einzelnen Firma
- `TriggerWkoCompanySyncRequest` / `TriggerWkoCompanySyncResponse` - Manuellen Sync auslösen
- `GetWkoCompanySyncStatusRequest` / `GetWkoCompanySyncStatusResponse` - Sync-Status abfragen

### DTOs

- `WkoCompanyDto` - Vollständige Firmen-Details
- `WkoCompanyPermitDto` - Einzelne Gewerbeberechtigung (Fachgruppe, Gewerbewortlaut, gewerberechtliche Geschäftsführung, GISA-Zahl)
- `FirmenbuchPersonDto` - Person laut amtlichem Firmenbuch-Auszug (Name, Geburtsdatum, Funktion) - leer solange kein Firmenbuch-HVD-API-Key konfiguriert ist

## Filter-Optionen

`GetWkoCompaniesRequest` unterstützt folgende Filter:

- `City`: Nach Ort filtern
- `PostalCode`: Nach Postleitzahl filtern
- `SearchText`: Volltextsuche über Name/Kategorie
- `IsActive`: Nur aktive (noch auf firmen.wko.at gelistete) Firmen
- `FoundedFrom`: Nur Firmen mit Gründungsdatum (frühestes Berechtigungs-"Seit"-Datum) ab X
- `FirstSeenFrom`: Nur Firmen, die ab Zeitpunkt X erstmals gescraped wurden

## Abhängigkeiten

- `Shiny.Mediator.Contracts`

## Verwendung

```csharp
// Beispiel: Alle aktiven Firmen in Linz abrufen
var request = new GetWkoCompaniesRequest(City: "Linz", IsActive: true);
var response = await mediator.Send(request);

foreach (var company in response.Companies)
{
    Console.WriteLine($"{company.Name} - {company.Email}");
}
```
