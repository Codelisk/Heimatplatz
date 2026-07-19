# Heimatplatz.Api.Features.Telemetry.Contracts

Request/Response-DTOs des Telemetry-Features (OpenTelemetry-Logging in die eigene Datenbank).

## Zweck

- Mediator-Contracts fuer die admin-geschuetzten Auswertungs-Endpoints (Fehlergruppen,
  Log-Suche, Trace-Detail, Statistik)
- Contract fuer die anonyme Client-Ingestion (`IngestClientLogsRequest`, MAUI-Crash-Reports)
- Gemeinsame Modelle: `ErrorGroupStatus`, `TelemetrySource`, DTOs fuer Logs/Spans/Gruppen

## Requests

| Request | HTTP (im Feature-Projekt) | Zweck |
|---------|---------------------------|-------|
| `ListErrorGroupsRequest` | GET `/api/telemetry/error-groups` | Fehlergruppen-Liste (Filter/Sort/Paging) |
| `GetErrorGroupDetailRequest` | GET `/api/telemetry/error-groups/{Id}` | Gruppe + letzte Auftreten |
| `SetErrorGroupStatusRequest` | POST `/api/telemetry/error-groups/status` | Triage-Status setzen (Id im Body - POST-Route-Parameter binden im Generator nicht) |
| `SearchTelemetryLogsRequest` | GET `/api/telemetry/logs` | Log-Suche (paged) |
| `GetTraceDetailRequest` | GET `/api/telemetry/traces/{TraceId}` | Spans + Logs eines Traces |
| `GetTelemetryStatsRequest` | GET `/api/telemetry/stats` | Fehler/Tag + Top-Gruppen |
| `IngestClientLogsRequest` | POST `/api/telemetry/client-logs` | Client-Fehler-Batch (anonym) |

## Konventionen

- Zeitfilter in GET-Requests sind ISO-8601-**Strings** (DateTimeOffset-Query-Parameter
  serialisiert der generierte Shiny-HTTP-Client kulturabhaengig und ist serverseitig
  nicht bindbar - siehe `GetPropertyChangesRequest`).

## Abhaengigkeiten

- `Shiny.Mediator.Contracts`
