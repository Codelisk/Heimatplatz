# Heimatplatz.Api.Features.SrealListings

Scraper und Sync-Service fuer sreal.at Immobilienangebote (Haeuser, Grundstuecke, Ferienimmobilien) in Oberoesterreich.

## Architektur

- **SrealScraper** — AngleSharp-basierter HTML-Scraper fuer sreal.at Suchergebnisse und Detailseiten
- **SrealSyncService** — Orchestriert den Sync: Scrape → Vergleich → Upsert → Change-Log
- **SrealPropertySyncService** — Mappt SrealListings auf die zentrale Properties-Tabelle

## API-Endpoints

| Methode | Pfad | Beschreibung |
|---------|------|-------------|
| POST | `/api/sreal-listings/sync` | Sync manuell triggern (Fire-and-forget) |
| GET | `/api/sreal-listings/sync/status` | Status des letzten Syncs |
| GET | `/api/sreal-listings` | Paginierte Rohdaten-Abfrage |

## Konfiguration

```json
{
  "SrealListings": {
    "Scraping": {
      "BaseUrl": "https://www.sreal.at",
      "TimeoutSeconds": 30,
      "DelayBetweenRequestsMs": 1500
    }
  }
}
```

## Abhaengigkeiten

- Core.Data, Core.Data.Seeding
- Properties, Locations, Auth, Notifications
- ForeclosureAuctions (AustrianState Enum, SystemUser)
- AngleSharp (HTML-Parsing)
