# Heimatplatz.Api

ASP.NET-Host der Heimatplatz-API: Programmstart, HTTP-Pipeline und Querschnitts-Konfiguration.

## Verantwortlichkeiten

- **Program.cs**: Kestrel/Middleware-Pipeline, JWT-Authentifizierung (inkl. Guard gegen
  unsichere Signaturschluessel), Authorization-Policies (`RequireSeller`, `RequireAdmin`),
  Rate-Limiting, CORS, OpenAPI, Forwarded-Headers, Bild-Proxy (`/api/images/proxy`)
- **Exception-Handler**: `ApiExceptionHandler` (fachliche `ApiException` → definierter
  Statuscode), `UnauthorizedExceptionHandler` (401), `LegacyExceptionHandler`
  (Sicherheitsnetz: `ArgumentException` → 400, `KeyNotFoundException` → 404)
- **wwwroot/uploads**: Ablage hochgeladener Inserats-Medien (im Docker-Deploy ein Volume)

## Abhaengigkeiten

- `Heimatplatz.Api.Core.Startup` (`AddApiServices`: DI, Mediator, DbContext, Features, Seeding)
- Alle Feature-Projekte werden ueber Startup registriert; die Mediator-Endpoints kommen aus
  den Source-generierten `MediatorEndpoints` der Features

## Konfiguration

`appsettings.json` / Umgebungsvariablen (siehe `deploy/hetzner/docker-compose.yml`):
`ConnectionStrings:DefaultConnection`, `Database:*` (Provider/AutoMigrate/EnableSeeding),
`Authentication:Jwt:*`, `Api:PublicBaseUrl`, `AiConnector:*`, `PushNotifications:*`,
`ForeclosureAuctions:Scraping:*` (inkl. `SyncTriggerKey` fuer den manuellen Edikte-Sync).

## Start (lokal)

```bash
dotnet run --project src/api/src/Heimatplatz.Api
# Swagger/OpenAPI: http://localhost:5292/openapi/v1.json, Health: /health
```
