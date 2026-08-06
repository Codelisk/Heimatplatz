# Heimatplatz.Api.Features.Dashboards.Contracts

Request/Response-DTOs und Modelle des Dashboards-Features ("Meine Uebersicht"):
Der Nutzer beschreibt in Freitext, wonach er sucht und wie er es sehen moechte,
die KI entwirft daraus eine persoenliche Uebersicht.

Konzept: `docs/ki-dashboard-konzept.md` im Repo-Root.

## Modelle

- **`DashboardDefinition`** - das zentrale Server-Driven-UI-Format: versioniertes
  JSON aus Bausteinen des festen Widget-Katalogs (`DashboardWidgetKinds`:
  `property-list`, `stat-row`, `map`, `highlight`, `new-listings`, `text-note`).
  Die KI liefert nie Code, nur diese Struktur; Web und MAUI sind reine Renderer.
  Clients lesen tolerant (unbekannte Kinds ueberspringen, unbekannte Felder ignorieren).
- **`DashboardPropertyQuery`** - einheitliche Datenauswahl aller immobilienbasierten
  Widgets (dieselben Achsen wie `PropertyQueryFilters`). `Locations` = Freitext von
  der KI, `MunicipalityIds` = serverseitig aufgeloest (nie von der KI).
- **`WidgetDataDto`** - anzeigefertige Widget-Daten der Daten-Ebene. Nullable-typisierte
  Payload-Felder statt Polymorphie (OpenAPI-/MAUI-Generator-Kompatibilitaet); je nach
  Kind ist genau ein Payload gesetzt, `highlight`/`new-listings` nutzen `PropertyList` mit.
- **`DashboardGenerationStatus`** - Queued/InProgress/Finished/Failed (eigene Spalten
  am Dashboard, nie im Definition-JSON).

## Requests (alle unter `/api/dashboards`, Bearer-Auth)

| Request | Route | Zweck |
|---------|-------|-------|
| `GenerateDashboardRequest` | `POST /generate` | Wunsch -> neues Dashboard (Queued) + KI-Job |
| `GetDashboardsRequest` | `GET /` | Liste des Nutzers |
| `GetDashboardRequest` | `GET /{Id}` | Status + Definition (Polling-Endpoint) |
| `RefineDashboardRequest` | `POST /refine` | Verfeinerungsrunde auf Basis der aktuellen Definition |
| `RevertDashboardRequest` | `POST /revert` | vorherige Fassung zurueckspielen (ohne KI) |
| `UpdateDashboardRequest` | `PUT /` | umbenennen |
| `DeleteDashboardRequest` | `DELETE /{Id}` | loeschen inkl. Revisionen |
| `GetDashboardDataRequest` | `GET /{Id}/data` | Daten-Ebene: alle Widget-Queries aufloesen (fail-soft) |

POST-/PUT-Requests tragen die Id im Body (Shiny-Mediator-OpenAPI-Generator-
Kompatibilitaet, gleiche Praezedenz wie `GenerateDraftDescriptionRequest`).

## Abhaengigkeiten

- `Heimatplatz.Api.Features.Properties.Contracts` (`PropertyListItemDto`,
  `PropertyMapPinDto` in den Widget-Payloads)
- `Shiny.Mediator.Contracts`
