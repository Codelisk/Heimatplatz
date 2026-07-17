# Heimatplatz.Api.Features.PropertyDrafts.Contracts

Request/Response-DTOs fuer das PropertyDrafts-Feature (server-seitige Inserat-Entwuerfe
des Erstellungs-Wizards in der MAUI-App).

## Zweck

Ein Entwurf haelt den vollstaendigen Wizard-Zustand einer angefangenen Immobilie
(Medien-URLs, Diktat, KI-Analyse-Referenz, Lage/Preis, Eckdaten) als flexiblen
JSON-Payload (`PropertyDraftData`). Pro angefangener Immobilie existiert ein Entwurf;
ein Nutzer kann mehrere Entwuerfe parallel haben.

## Requests

| Request | HTTP | Beschreibung |
|---|---|---|
| `SavePropertyDraftRequest` | `POST /api/property-drafts/` | Upsert (Id im Body; ohne Id = neu) |
| `GetPropertyDraftsRequest` | `GET /api/property-drafts/` | Liste (nur Summary-Daten) |
| `GetPropertyDraftRequest` | `GET /api/property-drafts/{Id}` | Einzelner Entwurf inkl. Payload |
| `DeletePropertyDraftRequest` | `DELETE /api/property-drafts/{Id}` | Loeschen inkl. Medien-Dateien |
| `PublishPropertyDraftRequest` | `POST /api/property-drafts/publish` | Serverseitig via CreateProperty veroeffentlichen |

Alle Endpoints erfordern die `RequireSeller`-Policy und sind auf den angemeldeten
Nutzer gescoped.

## Models

- `PropertyDraftData` — kompletter Wizard-Zustand, alle Felder optional
  (validiert wird erst beim Publish durch `CreatePropertyHandler`).
  `SchemaVersion` erlaubt tolerante Deserialisierung aelterer Entwuerfe.

## Abhaengigkeiten

- `Heimatplatz.Api.Features.Properties.Contracts` — `PropertyType`-Enum
- `Shiny.Mediator.Contracts`
