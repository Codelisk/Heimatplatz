# Heimatplatz.Api.Features.WkoCompanies

Scraped Firmendaten aus dem WKO Firmen-A-Z-Verzeichnis (firmen.wko.at) fuer die
konfigurierte Branche/Region (Default: Immobilienmakler/-treuhänder/-verwalter in
Oberösterreich).

## Zweck

Dieses Feature durchsucht firmen.wko.at nach Firmen der Immobilienbranche in
Oberösterreich und haelt einen aktuellen, lokalen Datenbestand (Name, Adresse,
Kontaktdaten, Gewerbeberechtigungen) - z.B. als Grundlage fuer B2B-Kontaktaufnahme.

## Warum ein eigener Scraper (kein simpler GET)?

Im Gegensatz zur Ediktsdatei (`ForeclosureAuctions`) ist firmen.wko.at eine klassische
ASP.NET-WebForms-Seite. Die "Mehr laden"-Pagination laeuft ueber einen vollstaendigen
AJAX-UpdatePanel-Postback (kompletter `__VIEWSTATE` im Body, laengenpraefixierte
"Delta"-Antwort statt JSON/HTML) - siehe `AspNetAjaxDeltaParser`. Auf einer einzelnen
Trefferliste sind Name, Adresse, Telefon, E-Mail und Website bereits als
schema.org-Microdata (`itemprop`) vorhanden; die Detailseite liefert zusaetzlich
Firmenbuchnummer, Rechtsform, Gruendungsjahr und Gewerbeberechtigungen.

## Verantwortlichkeiten

- **Scraping**: Multi-Keyword-Suche (`WkoScrapingOptions.BranchKeywords`) pro Region,
  Ergebnisse werden anhand der WKO-Firmaid dedupliziert
- **Datenverwaltung**: Speicherung als `WkoCompany` (Kontaktdaten, Firmendaten,
  Gewerbeberechtigungen als JSON-Liste)
- **Change-Detection**: SHA256-Content-Hash wie bei `ForeclosureAuctions` - unveraenderte
  Firmen werden nicht neu geschrieben, verschwundene Firmen werden soft-deleted
  (`IsActive=false`), keine Hard-Deletes
- **Inkrementeller Sync (Default)**: bekannte Firmen (WKO-Firmaid bereits in der DB) bekommen
  beim erneuten Lauf keinen Detailseiten-Request - nur neue Firmen werden voll gescraped
  (`SkipDetailsForKnownCompanies`, s.u.)
- **Gruendungsdatum**: `FoundedDate` = fruehestes "Seit"-Datum der Gewerbeberechtigungen
  (WKO-Hinweis: "kann vom Gruendungsdatum abweichen"); abfragbar ueber den
  `FoundedFrom`-Filter ("alle Firmen ab Datum X"), alternativ `FirstSeenFrom`
  fuer "seit wann bei uns bekannt"
- **Seeding**: 8 realistische (fiktive) Testeintraege fuer die lokale Entwicklung

## Datenmodell

### Entity: `WkoCompany`

Erbt von `BaseEntity` (Id, CreatedAt, UpdatedAt). Wichtigste Felder: `Name`,
`CategoryText`, `Street`/`PostalCode`/`City`, `Phones` (JSON-Liste), `Email`,
`Website`, `Permits` (JSON-Liste aus `WkoCompanyPermit`: Fachgruppe,
Gewerbewortlaut, gewerberechtliche Geschaeftsfuehrung, GISA-Zahl), `WkoFirmaId`
(Natural Key, unique), `IsActive`/`FirstSeenAt`/`LastScrapedAt`/`RemovedAt`.

## Öffentliche APIs

```csharp
GetWkoCompaniesRequest / GetWkoCompaniesResponse       // Liste mit Filtern + Paging
GetWkoCompanyByIdRequest / GetWkoCompanyByIdResponse   // Einzelne Firma
TriggerWkoCompanySyncRequest / ...Response             // Sync manuell ausloesen
GetWkoCompanySyncStatusRequest / ...Response           // Sync-Status (Zaehler)
```

## Abhängigkeiten

- `Heimatplatz.Api.Features.WkoCompanies.Contracts`
- `Heimatplatz.Api.Core.Data` (BaseEntity, AppDbContext)
- `Heimatplatz.Api.Core.Data.Seeding` (ISeeder)
- `Heimatplatz.Api.Shared` (DI-Konstanten, SharedKeyAuthorization)
- `AngleSharp` (HTML-Parsing)
- `Shiny.Mediator`

## Konfiguration

Abschnitt `WkoCompanies:Scraping` (`WkoScrapingOptions`):

| Feld | Default | Beschreibung |
|------|---------|--------------|
| `BaseUrl` | `https://firmen.wko.at` | |
| `Region` | `oberösterreich` | URL-Pfadsegment, wie von firmen.wko.at verwendet |
| `BranchKeywords` | Immobilienmakler(in), Immobilientreuhänder(in), Immobilienverwaltung, Immobilienbüro | Suchbegriffe, ueber Firmaid dedupliziert |
| `DelayBetweenRequestsMs` | `1500` | Rate-Limit zwischen JEDEM Request (Pagination UND Detailseiten) - firmen.wko.at antwortet bei zu dichten Requests mit HTTP 429 |
| `SkipDetailsForKnownCompanies` | `true` | Inkrementeller Modus: nur NEUE Firmen bekommen einen Detailseiten-Request; `false` = jeder Lauf aktualisiert alle Firmen (Content-Hash) |
| `MaxPagesPerKeyword` | `50` | Sicherheitsgrenze fuer die "Mehr laden"-Pagination pro Suchbegriff |
| `SyncIntervalHours` | `0` (deaktiviert) | Optionaler automatischer Hintergrund-Sync |
| `SyncTriggerKey` | - | Shared-Key fuer `POST /api/wko-companies/sync` (Header `X-Sync-Key`), fail-closed ausserhalb Development |

## Verwendung

### Registrierung

```csharp
services.AddWkoCompaniesFeature(configuration);
```

Wird automatisch in `Core.Startup/ServiceCollectionExtensions.cs` aufgerufen.

### Sync (Scraping)

- **Manuell (Standard)**: `POST /api/wko-companies/sync` mit Header `X-Sync-Key`
  (Konfiguration `WkoCompanies:Scraping:SyncTriggerKey`). Ohne konfigurierten Key
  ausserhalb von Development gesperrt (fail-closed). Parallele Laeufe werden
  abgewiesen (In-Process-Guard), laeuft fire-and-forget im Hintergrund. Status via
  `GET /api/wko-companies/sync/status`.
- **Optional automatisch**: `WkoCompanySyncWorker` (BackgroundService), Konfiguration
  `WkoCompanies:Scraping:SyncIntervalHours` (Default `0` = deaktiviert).

**Hinweis fuer die Produktions-Einbindung** (Env-Var-Mapping in
`deploy/hetzner/docker-compose.yml`/`.env.example`, Caddy-Freigabe des Endpoints
analog zum ForeclosureAuctions-Sync): bewusst noch nicht Teil dieses Commits, siehe
Projektnotizen.

## Datenquelle

https://firmen.wko.at (WKO Firmen A-Z)
