# Heimatplatz.Api.Features.PropertyDrafts.Contracts

Request/Response-DTOs fuer das PropertyDrafts-Feature (server-seitige Inserat-Entwuerfe
des Erstellungs-Wizards in der MAUI-App).

## Zweck

Ein Entwurf haelt den vollstaendigen Wizard-Zustand einer angefangenen Immobilie
(Foto-URLs, Eckdaten, Lage/Preis, Beschreibung) als flexiblen JSON-Payload
(`PropertyDraftData`). Pro angefangener Immobilie existiert ein Entwurf; ein Nutzer
kann mehrere Entwuerfe parallel haben. Die optionale KI-Beschreibungs-Generierung
laeuft asynchron als Hintergrund-Job; ihr Zustand liegt in eigenen Entwurfs-Spalten
(nicht im Payload) und wird ueber eigene Requests angefordert/gepollt.

## Requests

| Request | HTTP | Beschreibung |
|---|---|---|
| `SavePropertyDraftRequest` | `POST /api/property-drafts/` | Upsert (Id im Body; ohne Id = neu) |
| `GetPropertyDraftsRequest` | `GET /api/property-drafts/` | Liste (nur Summary-Daten) |
| `GetPropertyDraftRequest` | `GET /api/property-drafts/{Id}` | Einzelner Entwurf inkl. Payload + Beschreibungs-Zustand |
| `DeletePropertyDraftRequest` | `DELETE /api/property-drafts/{Id}` | Loeschen inkl. Medien-Dateien |
| `PublishPropertyDraftRequest` | `POST /api/property-drafts/publish` | Serverseitig via CreateProperty veroeffentlichen |
| `GenerateDraftDescriptionRequest` | `POST /api/property-drafts/generate-description` | KI-Beschreibung aus Stichwoertern anfordern (Id im Body, Hintergrund-Job) |
| `GetDraftDescriptionRequest` | `GET /api/property-drafts/{Id}/description` | Fortschritt der Generierung pollen |

Alle Endpoints erfordern die `RequireSeller`-Policy und sind auf den angemeldeten
Nutzer gescoped.

## Models

- `PropertyDraftData` — kompletter Wizard-Zustand, alle Felder optional
  (validiert wird erst beim Publish durch `CreatePropertyHandler`).
  `SchemaVersion` erlaubt tolerante Deserialisierung aelterer Entwuerfe
  (Version 2 = manueller Wizard-Fluss, KI-Analyse-Felder entfernt).
- `DraftDescriptionMode` — vom Nutzer gewaehlter Beschreibungs-Modus
  (`None`/`Manual`/`Generate`), Teil des Payloads.
- `DraftDescriptionStatus` — Job-Lebenszyklus der Generierung
  (`None`, `Queued`, `InProgress`, `Finished`, `Failed`).

## Abhaengigkeiten

- `Heimatplatz.Api.Features.Properties.Contracts` — `PropertyType`-Enum
- `Shiny.Mediator.Contracts`
