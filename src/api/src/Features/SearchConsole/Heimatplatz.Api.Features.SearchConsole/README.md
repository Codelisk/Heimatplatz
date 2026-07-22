# Heimatplatz.Api.Features.SearchConsole

Suchperformance-Kennzahlen (Klicks, Impressionen, CTR, Position, Top-Suchbegriffe/-Seiten)
aus der Google Search Console fuer den Intern-Bereich (`/intern/analytics`). Getrenntes
Thema vom Rybbit-Traffic-Analytics (laeuft auf einem eigenen Server, analytics.heimatplatz.at,
verlinkt auf derselben Intern-Seite) - Search Console misst die echte Google-Suche, nicht
den Traffic auf der eigenen Seite.

## Endpoints

| Methode | Pfad | Handler |
|---------|------|---------|
| GET | `/api/admin/search-console/summary` | `GetSearchConsoleSummaryHandler` |

## Sicherheit: Shared-Key statt JWT

Wie alle `/api/admin`-Endpoints per `X-Admin-Key`-Header (`IAdminAccessGuard`, siehe
`Heimatplatz.Api.Features.Admin`) - kein eigenes Auth-Konzept.

## Konfiguration

Server-zu-Server-Auth ueber einen Google-Service-Account-JSON-Key - **kein interaktiver
OAuth-Consent-Flow**, gleiches Prinzip wie der Firebase-Service-Account im
Notifications-Feature.

```json
{
  "SearchConsole": {
    "ServiceAccountPath": "search-console-service-account.json",
    "SiteUrl": "sc-domain:heimatplatz.at"
  }
}
```

Einrichtung (einmalig, nur ueber die Google Cloud Console / Search Console moeglich):

1. In der Google Cloud Console: Projekt waehlen, **Search Console API** aktivieren, ein
   **Service Account** anlegen und einen JSON-Key generieren.
2. In der Search Console (`search.google.com/search-console`) unter Einstellungen ->
   Nutzer und Berechtigungen: die Service-Account-E-Mail
   (`...@...iam.gserviceaccount.com`) als Nutzer mit **"Eingeschraenkt"** (nur lesen)
   zur Property hinzufuegen.
3. Den JSON-Key lokal neben `appsettings.json` ablegen (Pfad wie oben, gitignored) bzw.
   auf dem Server manuell unter `deploy/hetzner/secrets/` (siehe
   `deploy/hetzner/docker-compose.yml`, gleiches Muster wie die Firebase/APNs-Secrets -
   es gibt keinen automatisierten Deploy-Schritt dafuer).

Ohne konfigurierten `ServiceAccountPath` bleibt das Feature **fail-soft**: der Endpoint
liefert `Enabled: false` statt eines Fehlers, `/intern/analytics` zeigt dann nur einen
"nicht konfiguriert"-Hinweis.

## Abhaengigkeiten

- `Heimatplatz.Api.Features.SearchConsole.Contracts` (Requests/DTOs)
- `Heimatplatz.Api.Features.Admin` (`IAdminAccessGuard`)
- `Heimatplatz.Api.Shared` (`ApiService`-DI-Konstanten)
- `Google.Apis.SearchConsole.v1` (offizieller Google-API-Client)
