# Heimatplatz.Api.Features.Admin.Contracts

Request/Response-DTOs (Shiny-Mediator-Contracts) fuer das Admin-Feature - die Endpoints
hinter dem Intern-Bereich des Astro-Webs (`/intern`).

## Requests

| Request | Endpoint | Zweck |
|---------|----------|-------|
| `GetAdminStatsRequest` | `GET /api/admin/stats` | Dashboard-Kennzahlen (Nutzer, Inserate, Ausgeblendete) |
| `GetAdminUsersRequest` | `GET /api/admin/users` | Nutzerliste mit Suche + Paging, neueste zuerst |
| `GetAdminPropertiesRequest` | `GET /api/admin/properties` | Inseratsliste mit Filtern (Quelle, Status, Suche, Nutzer) |
| `SetPropertyVisibilityRequest` | `POST /api/admin/properties/visibility` | Inserat aus-/einblenden (`Property.IsHidden`) |
| `AdminDeletePropertyRequest` | `DELETE /api/admin/properties/{Id}` | Inserat endgueltig loeschen (inkl. Upload-Bilder) |

## Sicherheit

Alle Endpoints verlangen den Shared-Key-Header `X-Admin-Key` (siehe `AdminOptions` im
Hauptprojekt) - kein JWT, weil es auf Prod keinen echten Admin-Account gibt. Details im
README des Hauptprojekts `Heimatplatz.Api.Features.Admin`.

## Abhaengigkeiten

- `Heimatplatz.Api.Shared` (`SellerType`)
- `Heimatplatz.Api.Features.Properties.Contracts` (`PropertyType`)
