# Heimatplatz.Api.Features.OpenImmoImport.Contracts

Request/Response-DTOs des OpenImmo-Import-Features (Maklersoftware-Feeds wie Justimmo
liefern Objektbestaende als OpenImmo-XML per FTP-Push, siehe Hauptprojekt-README).

## Requests

| Request | Endpoint | Zweck |
|---------|----------|-------|
| `TriggerOpenImmoImportRequest` | `POST /api/openimmo-import/sync` | Import manuell starten (fire-and-forget, `Force` umgeht den Marker-Kurzschluss) |
| `GetOpenImmoImportStatusRequest` | `GET /api/openimmo-import/status` | Letzter Lauf + Property-Bestand je Feed, `IsRunning` |

Beide Endpoints sind ueber den Shared-Key-Header `X-Sync-Key` geschuetzt
(fail-closed ausserhalb von Development, siehe `SharedKeyAuthorization`).

## Abhaengigkeiten

- `Shiny.Mediator.Contracts`
