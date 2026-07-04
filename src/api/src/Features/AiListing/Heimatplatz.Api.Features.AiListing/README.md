# Heimatplatz.Api.Features.AiListing

KI-gestuetzte Erstellung von Immobilien-Inseraten: Der Nutzer laedt Fotos/Videos
hoch und diktiert eine Beschreibung; ein Hintergrund-Job extrahiert daraus die
Inseratsfelder (Titel, Beschreibung, Typ, Zimmer, Flaechen, Baujahr, Ausstattung).

**Bewusst NICHT per KI befuellt:** Preis, Adresse, Gemeinde, Verkaeufer-/Kontaktdaten —
diese werden manuell in der App erfasst.

## Ablauf

1. `POST /api/ai-listings/media` — Fotos/Videos als Base64 hochladen (idealerweise
   eine Datei pro Request), gespeichert unter `wwwroot/uploads/listings/`.
2. `POST /api/ai-listings` — Analyse starten. Legt einen `ListingAnalysis`-Job an
   (Status `Queued`) und reiht ihn in die `ListingAnalysisQueue` ein.
3. `ListingAnalysisWorker` (BackgroundService) verarbeitet die Queue:
   `Queued` → `InProgress` → `Finished` (Ergebnis als JSON) oder `Failed`.
4. `GET /api/ai-listings/{AnalysisId}` — Status-Polling; bei `Finished` enthaelt
   die Response das `ExtractedListingData`.
5. Die App zeigt das Ergebnis zur Review und erstellt das Inserat anschliessend
   ueber den regulaeren `POST /api/properties` Endpoint.

Alle Endpoints erfordern die Rolle `Seller`.

## Extraktions-Provider (`IListingExtractionService`)

| Provider | Klasse | Verwendung |
|----------|--------|------------|
| `Mock` (Default) | `MockListingExtractionService` | Dev: heuristische Extraktion aus dem Diktat (Regex fuer Zimmer/m²/Baujahr, Feature-Keywords), simulierte Laufzeit |
| `Cli` | `CliListingExtractionService` | Server: ruft eine installierte Agent-CLI auf (z.B. `claude` oder `codex`). Prompt via stdin, Mediendateipfade im Prompt, Antwort muss ein einzelnes JSON-Objekt im `ExtractedListingData`-Schema sein |

## Konfiguration (`AiListing` Section)

```json
{
  "AiListing": {
    "Provider": "Cli",
    "CliCommand": "claude",
    "CliArguments": "-p --output-format text",
    "WorkingDirectory": "/home/site",
    "TimeoutSeconds": 300,
    "MaxImages": 20,
    "MaxVideos": 3,
    "MaxVideoSizeMb": 60
  }
}
```

Default ist `Provider: "Mock"` (siehe `appsettings.json`), damit der Flow lokal
ohne installierte CLI funktioniert.

## Abhaengigkeiten

- `Heimatplatz.Api.Features.AiListing.Contracts` — Request/Response DTOs
- `Heimatplatz.Api.Core.Data` — `AppDbContext`, `BaseEntity` (Entity `ListingAnalysis` wird auto-discovered)
- `Heimatplatz.Api.Shared` — `ApiService` DI-Konstanten, `AuthorizationPolicies`

## Hinweise

- Die Queue ist in-memory; nach einem Neustart reiht der Worker offene Jobs
  (`Queued`/`InProgress`) aus der Datenbank erneut ein.
- Videos werden aktuell an die CLI nur als Dateipfad uebergeben; kann die CLI
  keine Videos lesen, ignoriert sie diese laut Prompt.
