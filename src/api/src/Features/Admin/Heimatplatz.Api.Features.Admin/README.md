# Heimatplatz.Api.Features.Admin

Endpoints fuer den Intern-Bereich des Astro-Webs (`/intern`): Nutzeruebersicht,
Inseratsverwaltung (Nutzer-Inserate und Zwangsversteigerungen) und Dashboard-Kennzahlen.
Kernfunktion ist die Moderation ueber `Property.IsHidden` - ausgeblendete Inserate
verschwinden aus allen oeffentlichen Abfragen und werden ueber den Delta-Sync als
Deleted-Tombstone an die MAUI-Clients ausgeliefert (siehe `PropertyChangeInterceptor`
im Properties-Feature).

## Endpoints

| Methode | Pfad | Handler |
|---------|------|---------|
| GET | `/api/admin/stats` | `GetAdminStatsHandler` |
| GET | `/api/admin/users` | `GetAdminUsersHandler` |
| GET | `/api/admin/properties` | `GetAdminPropertiesHandler` |
| POST | `/api/admin/properties/visibility` | `SetPropertyVisibilityHandler` |
| DELETE | `/api/admin/properties/{Id}` | `AdminDeletePropertyHandler` |

## Sicherheit: Shared-Key statt JWT

Alle Handler pruefen zu Beginn den Header `X-Admin-Key` gegen `Admin:ApiKey`
(`AdminAccessGuard`, timing-sicherer Vergleich). Shared-Key statt `RequireAdmin`-Policy,
weil es auf Prod keinen echten Admin-Account gibt (nur der Properties-System-User ohne
Passwort/Rolle) - gleiches Muster wie der Edikte-Sync-Trigger (`X-Sync-Key`).

- Ohne konfigurierten Key sind die Endpoints ausserhalb von Development gesperrt (fail-closed).
- Aufrufer ist ausschliesslich der Astro-SSR-Server (Intern-Seiten + Server-APIRoutes),
  der den Key aus der Umgebungsvariable `ADMIN_API_KEY` mitschickt
  (`deploy/hetzner/docker-compose.yml`).
- Defense-in-depth: Caddy blockt `/api/admin*` auf den oeffentlichen API-Domains fuer
  alles ausser `HOME_IP` (`deploy/hetzner/Caddyfile`); der SSR-Server erreicht die API
  ueber das interne Docker-Netz.

## Konfiguration

```json
{
  "Admin": {
    "ApiKey": "<zufaelliger Wert, z.B. openssl rand -hex 32>"
  }
}
```

## Abhaengigkeiten

- `Heimatplatz.Api.Features.Admin.Contracts` (Requests/DTOs)
- `Heimatplatz.Api.Core.Data` (AppDbContext)
- `Heimatplatz.Api.Features.Properties` (Property-Entity, `IPropertyImageService`, URL-Proxy-Helfer)
- `Heimatplatz.Api.Features.Auth` (User-Entity)
