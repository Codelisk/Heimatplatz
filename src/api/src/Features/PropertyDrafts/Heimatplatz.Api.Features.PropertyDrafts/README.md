# Heimatplatz.Api.Features.PropertyDrafts

Server-seitige Inserat-Entwuerfe fuer den Erstellungs-Wizard der MAUI-App.

## Zweck und Verantwortlichkeiten

Ein `PropertyDraft` haelt den vollstaendigen Zustand einer angefangenen Immobilie
(Medien-URLs, Diktat, KI-Analyse-Referenz, Lage/Preis, Eckdaten). Pro angefangener
Immobilie existiert ein Entwurf; ein Nutzer kann mehrere Entwuerfe parallel haben
(max. 10). Der Wizard speichert bei jedem Schrittwechsel per Upsert.

**Bewusst KEINE Property-Zeile:** Entwuerfe sind eine eigene Entity und tauchen dadurch
nie im PropertyChange-Journal, im Delta-Sync (`GET /api/properties/changes`), in den
oeffentlichen Abfragen oder im `PropertyCreatedEvent` auf.

## Architektur

- **Entity `PropertyDraft`**: typisierte Summary-Spalten (Title, Type, StepIndex,
  FirstImageUrl, AnalysisId) nur fuer die Listen-Anzeige; der komplette Wizard-Zustand
  liegt als JSON-Blob in `PayloadJson` (`PropertyDraftData` aus dem Contracts-Projekt).
  Payload-Felderweiterungen brauchen daher keine Migration (`SchemaVersion` fuer
  tolerante Deserialisierung).
- **Publish**: `PublishPropertyDraftHandler` mappt den Payload serverseitig auf
  `CreatePropertyRequest` und schickt ihn in-process durch den Mediator - Validierung,
  `SellerInfoResolver`, Journal und Events feuern dadurch wie bei einer normalen
  Erstellung. Bei Erfolg wird nur die Entwurfs-Zeile geloescht (Medien bleiben, die
  Immobilie referenziert sie).
- **Delete**: loescht zusaetzlich die hochgeladenen Medien-Dateien via
  `IPropertyImageService.DeleteImageAsync` (deckt `uploads/listings` mit ab).
- **DSGVO**: `PropertyDraftsUserDataEraser` (Order 40) loescht bei Konto-Loeschung
  alle Entwuerfe + Medien.

## Endpoints (alle `RequireSeller`, gescoped auf den angemeldeten Nutzer)

| Route | Handler |
|---|---|
| `POST /api/property-drafts/` | `SavePropertyDraftHandler` (Upsert, Id im Body) |
| `GET /api/property-drafts/` | `GetPropertyDraftsHandler` |
| `GET /api/property-drafts/{Id}` | `GetPropertyDraftHandler` |
| `DELETE /api/property-drafts/{Id}` | `DeletePropertyDraftHandler` |
| `POST /api/property-drafts/publish` | `PublishPropertyDraftHandler` |

## Abhaengigkeiten

- `Heimatplatz.Api.Features.PropertyDrafts.Contracts` — DTOs/Requests
- `Heimatplatz.Api.Features.Properties` — `IPropertyImageService` (Medien-Cleanup),
  `CreatePropertyRequest` (Publish-Mapping)
- `Heimatplatz.Api.Core.Data` — `AppDbContext`, `BaseEntity`
- `Heimatplatz.Api.Shared` — `ApiService`-DI-Konstanten, `AuthorizationPolicies`, `IUserDataEraser`

## Registrierung

```csharp
services.AddPropertyDraftsFeature();
// Endpoints:
Heimatplatz.Api.Features.PropertyDrafts.MediatorEndpoints.MapGeneratedMediatorEndpoints(app);
```
