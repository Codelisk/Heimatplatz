# Heimatplatz.Api.Features.PropertyDrafts

Server-seitige Inserat-Entwuerfe fuer den Erstellungs-Wizard der MAUI-App,
inklusive des asynchronen Hintergrund-Jobs fuer die KI-Beschreibungs-Generierung.

## Zweck und Verantwortlichkeiten

Ein `PropertyDraft` haelt den vollstaendigen Zustand einer angefangenen Immobilie
(Foto-URLs, Eckdaten, Lage/Preis, Beschreibung). Pro angefangener Immobilie existiert
ein Entwurf; ein Nutzer kann mehrere Entwuerfe parallel haben (max. 10). Der Wizard
speichert bei jedem Schrittwechsel per Upsert.

**Bewusst KEINE Property-Zeile:** Entwuerfe sind eine eigene Entity und tauchen dadurch
nie im PropertyChange-Journal, im Delta-Sync (`GET /api/properties/changes`), in den
oeffentlichen Abfragen oder im `PropertyCreatedEvent` auf.

## Architektur

- **Entity `PropertyDraft`**: typisierte Summary-Spalten (Title, Type, StepIndex,
  FirstImageUrl) nur fuer die Listen-Anzeige; der komplette Wizard-Zustand liegt als
  JSON-Blob in `PayloadJson` (`PropertyDraftData` aus dem Contracts-Projekt).
  Payload-Felderweiterungen brauchen daher keine Migration (`SchemaVersion` fuer
  tolerante Deserialisierung; Version 2 = manueller Wizard-Fluss ohne KI-Analyse-Felder).
- **Beschreibungs-Generierung (asynchron)**: Die `Description*`-Spalten (Status,
  Keywords, GeneratedDescription, Error, Zeitstempel) liegen bewusst NICHT im Payload —
  Client-Auto-Saves (kompletter Payload-Upsert) koennen den Job-Fortschritt so nie
  ueberschreiben. `GenerateDraftDescriptionHandler` setzt Status `Queued` und plant
  ueber `IDraftDescriptionJobScheduler` einen **TickerQ-TimeTicker** ein
  (`DraftDescriptionJob`, Funktion `generate-draft-description`, 3 Retries mit
  30/120/300s). Der Job laeuft in einem eigenen DI-Scope und ruft den
  `DraftDescriptionProcessor` auf: Entwurf laden, `GenerateListingDescriptionRequest`
  in-process ans AiListing-Feature (Provider Mock/AiConnector, Wortbereich fix im
  Backend via `AiListing:Description`), Ergebnis in die Spalten schreiben. Beim letzten
  fehlgeschlagenen Versuch wird Status `Failed` + Fehlertext persistiert.
  Ohne echte Datenbank (Build-Zeit-OpenAPI-Generierung, Integrationstests mit
  InMemory-Provider) ist TickerQ nicht registriert und der
  `NoOpDraftDescriptionJobScheduler` aktiv.
- **Publish**: `PublishPropertyDraftHandler` mappt den Payload serverseitig auf
  `CreatePropertyRequest` und schickt ihn in-process durch den Mediator - Validierung,
  `SellerInfoResolver`, Journal und Events feuern dadurch wie bei einer normalen
  Erstellung. Ist im Payload keine Beschreibung, aber eine generierte vorhanden, wird
  die generierte uebernommen. Bei Erfolg wird nur die Entwurfs-Zeile geloescht
  (Medien bleiben, die Immobilie referenziert sie).
- **Delete**: loescht zusaetzlich die hochgeladenen Medien-Dateien via
  `IPropertyImageService.DeleteImageAsync` (deckt `uploads/listings` mit ab). Ein noch
  laufender Beschreibungs-Job findet den Entwurf dann nicht mehr und beendet sich leise.
- **DSGVO**: `PropertyDraftsUserDataEraser` (Order 40) loescht bei Konto-Loeschung
  alle Entwuerfe + Medien.

## Endpoints (alle `RequireSeller`, gescoped auf den angemeldeten Nutzer)

| Route | Handler |
|---|---|
| `POST /api/property-drafts/` | `SavePropertyDraftHandler` (Upsert, Id im Body) |
| `GET /api/property-drafts/` | `GetPropertyDraftsHandler` |
| `GET /api/property-drafts/{Id}` | `GetPropertyDraftHandler` (inkl. Beschreibungs-Zustand) |
| `DELETE /api/property-drafts/{Id}` | `DeletePropertyDraftHandler` |
| `POST /api/property-drafts/publish` | `PublishPropertyDraftHandler` |
| `POST /api/property-drafts/generate-description` | `GenerateDraftDescriptionHandler` (Id im Body) |
| `GET /api/property-drafts/{Id}/description` | `GetDraftDescriptionHandler` (Polling) |

## Abhaengigkeiten

- `Heimatplatz.Api.Features.PropertyDrafts.Contracts` — DTOs/Requests
- `Heimatplatz.Api.Features.Properties` — `IPropertyImageService` (Medien-Cleanup),
  `CreatePropertyRequest` (Publish-Mapping)
- `Heimatplatz.Api.Features.AiListing.Contracts` — `GenerateListingDescriptionRequest`
  (in-process Beschreibungs-Generierung)
- `Heimatplatz.Api.Core.Data` — `AppDbContext`, `BaseEntity`
- `Heimatplatz.Api.Shared` — `ApiService`-DI-Konstanten, `AuthorizationPolicies`, `IUserDataEraser`
- `TickerQ` — Job-Registrierung (`MapTicker`) + `ITimeTickerManager`

## Registrierung

```csharp
services.AddPropertyDraftsFeature(backgroundJobsEnabled);
// Endpoints:
Heimatplatz.Api.Features.PropertyDrafts.MediatorEndpoints.MapGeneratedMediatorEndpoints(app);
```
