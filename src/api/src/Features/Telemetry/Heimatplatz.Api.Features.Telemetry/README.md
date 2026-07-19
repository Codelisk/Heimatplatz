# Heimatplatz.Api.Features.Telemetry

OpenTelemetry-basiertes Logging-/Tracing-System, das in die eigene Datenbank persistiert
(Postgres prod/test, SQLite dev) und auf Fehlerauswertung optimiert ist. Kein externes
APM, kein OTLP-Export - zwei eigene OTel-Prozessoren + zentraler Batch-Writer.

## Architektur

```
ILogger ----> OTel LoggerProvider --> TelemetryLogProcessor --+--> TelemetryWriter --> AppDbContext
                                        |  (Warning+ sofort,  |      (Channel, Batch,
Activities -> OTel TracerProvider ----> |   Error markiert    |       SuppressInstrumentation,
              (AspNetCore/HttpClient/   |   den Trace)        |       ErrorGroup-Upsert)
               Npgsql, AlwaysOn)        v                     |
                                      TraceBufferService -----+
                                      (Spans + Info-Logs pro Trace,
                                       Tail-Entscheidung am Root)
```

### Tail-Sampling (Entscheidung beim Ende des lokalen Root-Spans)

Ein Trace wird komplett persistiert (Spans + nachgereichte Info/Debug-Kontext-Logs) wenn:

| Bedingung | Quelle |
|-----------|--------|
| Root-Span hat Error-Status | unbehandelte Exception, 5xx |
| Trace per Error-Log markiert | `TelemetryLogProcessor.MarkError` |
| Dauer > `SlowRequestThresholdMs` | langsame Requests |
| Zufalls-Stichprobe (`SampleHealthyTracePercent`) | Performance-Baseline |

Sonst wird der Puffer verworfen. Logs ab Warning werden IMMER persistiert (auch ohne
Trace). Lokaler Root = `Activity.Parent == null` - deckt auch Remote-Parents via
`traceparent` von Clients ab.

### Fehlergruppen

SHA-256-Fingerprint aus Exception-Typ + Top-5-Stackframes (Pfade/Zeilennummern
gestrippt) + Message-Template -> `TelemetryErrorGroup` (Count, First/LastSeen,
LastTraceId, Triage-Status). Gruppen werden nie geloescht.

### Fail-open-Garantie

Telemetrie darf nie einen Request brechen: bounded Channel mit DropWrite, Puffer-Caps
(`MaxBufferedTraces`/`MaxSpansPerTrace`/`MaxLogsPerTrace`), Writer schluckt DB-Fehler.
Feedback-Loops sind dreifach abgesichert: `SuppressInstrumentationScope` um den
Writer-Flush (unterdrueckt Npgsql-Spans UND ILogger-Records), Kategorie-Guard im
LogProcessor, `/health` aus der ASP.NET-Instrumentierung gefiltert.

## HTTP-Endpoints

| Endpoint | Auth | Zweck |
|----------|------|-------|
| GET `/api/telemetry/error-groups` | Admin | Fehlergruppen-Liste |
| GET `/api/telemetry/error-groups/{Id}` | Admin | Gruppe + letzte Auftreten |
| POST `/api/telemetry/error-groups/status` | Admin | Triage-Status setzen (Id im Body) |
| GET `/api/telemetry/logs` | Admin | Log-Suche (paged) |
| GET `/api/telemetry/traces/{TraceId}` | Admin | Spans + Logs eines Traces |
| GET `/api/telemetry/stats` | Admin | Fehler/Tag + Top-Gruppen |
| POST `/api/telemetry/client-logs` | anonym (Rate-Limit 20/min/IP) | MAUI-Crash-Reports |

## Konfiguration (Section `Telemetry`, alle Werte optional)

Siehe `Configuration/TelemetryOptions.cs`. Wichtig:

- **`Logging:OpenTelemetry:LogLevel`** (Provider-Override in appsettings) ist
  load-bearing: In Production steht das globale Default auf Warning - ohne den
  Override auf Information bekaeme der Trace-Puffer keine Kontext-Logs.
- Die gesamte Pipeline haengt am Connection-String-Gate (wie TickerQ): ohne echte DB
  (Build-Zeit-OpenAPI-Gen, InMemory-Integrationstests) sind nur die Handler registriert.
- `Telemetry:Enabled=false` schaltet die Pipeline auch mit DB ab.

## Bekannte Eigenheiten

- Unbehandelte 500er koennen doppelt geloggt werden (ASP.NET ExceptionHandlerMiddleware
  + FallbackExceptionHandler) - gleiche TraceId, der Fingerprint dedupliziert sie in
  der Gruppenansicht.
- ErrorGroup-Zaehler: Writer-Thread und Ingestion-Handler schreiben parallel - seltene
  verlorene Increments sind akzeptiert (Diagnostik, keine Buchhaltung).
- EF-Migrationen: nur das Postgres-Set (`Core.Data.Migrations.Postgres`) enthaelt die
  Telemetry-Tabellen; SQLite-Dev nutzt `EnsureCreated`. Ein reaktiviertes
  SqlServer-Deployment braeuchte eine Migration im Legacy-Set.

## Abhaengigkeiten

- `Heimatplatz.Api.Features.Telemetry.Contracts` (DTOs)
- `Heimatplatz.Api.Core.Data` (AppDbContext, BaseEntity)
- `Heimatplatz.Api.Shared` (ApiService, AuthorizationPolicies, ApiException)
- OpenTelemetry 1.17 (SDK, Hosting, AspNetCore-/Http-Instrumentierung), Npgsql.OpenTelemetry
