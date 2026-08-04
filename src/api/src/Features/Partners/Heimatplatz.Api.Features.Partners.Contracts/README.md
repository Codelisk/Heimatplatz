# Heimatplatz.Api.Features.Partners.Contracts

Request/Response-DTOs des Partners-Features (oeffentliche Partner-Seite `/partner/`
und Intern-Pflege `/intern/partner/`).

## Inhalte

- `Models/PartnerDto.cs` - Partner inkl. berechnetem `ActiveListingCount`
- `Models/PartnerCategories.cs` - Kategorie-Konstanten (`Broker`, `DataSource`)
- `Mediator/Requests/GetPartnersRequest.cs` - oeffentliche Liste (nur sichtbare)
- `Mediator/Requests/GetAdminPartnersRequest.cs` - vollstaendige Liste (Admin)
- `Mediator/Requests/SavePartnerRequest.cs` - Anlegen/vollstaendiges Ersetzen (Admin)
- `Mediator/Requests/DeletePartnerRequest.cs` - endgueltiges Loeschen (Admin)
- `Mediator/Requests/UploadPartnerLogoRequest.cs` - Logo-Upload via Bild-Pipeline (Admin)

## Konventionen

- Fachliche Fehler kommen als `Success=false` + `Error`-Text (HTTP 200), damit die
  Intern-Seite konkrete Meldungen zeigen kann (Muster von `UpdateContactSettingsResponse`).
- `Category` ist bewusst ein String mit Konstanten statt Enum - neue Kategorien
  brauchen keine Client-Aenderung.

## Abhaengigkeiten

- `Shiny.Mediator.Contracts`
- `Heimatplatz.Api.Features.Properties.Contracts` (`Base64ImageData` fuer den Logo-Upload)
