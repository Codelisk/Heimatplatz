# Heimatplatz.Api.Features.ForeclosureAuctions

Verwaltet Zwangsversteigerungsdaten aus der österreichischen Ediktsdatei.

## Zweck

Dieses Feature stellt Daten zu Immobilien-Zwangsversteigerungen bereit, die von österreichischen Gerichten veröffentlicht werden. Es ermöglicht das Suchen, Filtern und Abrufen von Versteigerungsinformationen.

## Verantwortlichkeiten

- **Datenverwaltung**: Speicherung von Zwangsversteigerungsdaten (Datum, Adresse, Objekttyp, Schätzwert, etc.)
- **Filterung**: Suche nach Bundesland, Kategorie, Ort, Postleitzahl, Versteigerungsdatum und geschätztem Wert
- **Seeding**: Bereitstellung realistischer Testdaten für alle österreichischen Bundesländer

## Datenmodell

### Entity: `ForeclosureAuction`

Erbt von `BaseEntity` (Id, CreatedAt, UpdatedAt) und erweitert um:

| Feld | Typ | Beschreibung |
|------|-----|--------------|
| `AuctionDate` | `DateTimeOffset` | Datum der Versteigerung |
| `Address` | `string` | Straße und Hausnummer |
| `City` | `string` | Ort/Stadt |
| `PostalCode` | `string` | Postleitzahl |
| `State` | `AustrianState` | Bundesland |
| `Category` | `PropertyCategory` | Kategorie der Liegenschaft |
| `ObjectDescription` | `string` | Bezeichnung des Objekts |
| `EdictUrl` | `string?` | URL zum vollständigen Edikt |
| `Notes` | `string?` | Zusätzliche Hinweise |
| `EstimatedValue` | `decimal?` | Geschätzter Wert |
| `MinimumBid` | `decimal?` | Mindestgebot |
| `CaseNumber` | `string?` | Aktenzeichen |
| `Court` | `string?` | Zuständiges Gericht |

### Enums

**AustrianState**: Burgenland, Kärnten, Niederösterreich, Oberösterreich, Salzburg, Steiermark, Tirol, Vorarlberg, Wien

**PropertyCategory**: Einfamilienhaus, Zweifamilienhaus, Mehrfamilienhaus, Wohnungseigentum, Gewerbliche Liegenschaft, Grundstück, Land- und Forstwirtschaft, Sonstiges

## Öffentliche APIs

### Contracts

```csharp
// Abrufen aller Versteigerungen mit Filtern
GetForeclosureAuctionsRequest
GetForeclosureAuctionsResponse

// Abrufen einer einzelnen Versteigerung
GetForeclosureAuctionByIdRequest
GetForeclosureAuctionByIdResponse
```

### DTOs

```csharp
ForeclosureAuctionDto
```

## Abhängigkeiten

- `Heimatplatz.Api.Features.ForeclosureAuctions.Contracts`
- `Heimatplatz.Api.Core.Data` (BaseEntity, AppDbContext)
- `Heimatplatz.Api.Core.Data.Seeding` (ISeeder)
- `Heimatplatz.Api.Shared` (DI-Konstanten)
- `Shiny.Mediator`

## Konfiguration

Keine besonderen Konfigurationsoptionen erforderlich.

## Verwendung

### Registrierung

```csharp
services.AddForeclosureAuctionsFeature();
```

Wird automatisch in `Core.Startup/ServiceCollectionExtensions.cs` aufgerufen.

### Seeding

Der `ForeclosureAuctionSeeder` erstellt automatisch 12 realistische Testeinträge für alle österreichischen Bundesländer beim ersten Start.

## Sync (Scraping)

- **Automatisch**: `ForeclosureAuctionSyncWorker` (BackgroundService) ruft den Sync alle
  `ForeclosureAuctions:Scraping:SyncIntervalHours` Stunden auf (Default 6, erster Lauf kurz
  nach dem Start). `0` oder negativ deaktiviert den automatischen Sync.
- **Manuell**: `POST /api/foreclosure-auctions/sync` (authentifiziert, `RequireAnyRole`) -
  läuft fire-and-forget im Hintergrund, Status via `GET /api/foreclosure-auctions/sync/status`.
- **Nur echte, künftige Versteigerungen werden übernommen**: Edikte ohne gültigen,
  in der Zukunft liegenden Versteigerungstermin (z.B. "Zuschlag mit/ohne Überbot",
  "Meistbotsverteilung", "Verschiebung" ohne neuen Termin - allesamt abgeschlossene
  Verfahren) werden nicht (mehr) als aktiv geführt; bereits vorhandene Einträge werden
  mit ChangeType `Concluded` deaktiviert. Kategorie-Ausschluss (`ExcludedCategories`)
  wird zusätzlich gegen den zuverlässigeren Detailseiten-Text geprüft, nicht nur gegen
  die grob geparste Listenseiten-Kategorie.
- **Kein Auto-Heal per Datenbank-Löschung**: Frühere Versionen haben bei bestimmten
  DB-Fehlern (Schema-Mismatch, Truncation) die komplette Datenbank gelöscht und leer neu
  angelegt - das ist entfernt. Echte Schema-Probleme gehören per EF-Migration behoben.

## Datenquelle

Die Struktur basiert auf der österreichischen Ediktsdatei:
https://edikte.justiz.gv.at/edikte/ex/exedi3.nsf/suchedi
