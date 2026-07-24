# Heimatplatz.Api.Features.Firmenbuch

Zugriff auf die amtliche **FBW-WebServices (HVD)**-Schnittstelle des Bundesministeriums fuer
Justiz (High Value Datasets des Firmenbuchs nach DVO (EU) 2023/138) sowie ein lokal
gespeicherter **Firmenbuch-Katalog** (Firmenstammdaten, unabhaengig von einer Branche).

## Zweck und Verantwortlichkeiten

- **`FirmenbuchHvdClient`** (`IFirmenbuchHvdClient`): SOAP-1.2-Client fuer die FBW-WebServices
  (`X-API-KEY`-Header = JustizOnline-IWG-Zugriffstoken). Operationen:
  - `GetAuszugAsync(fnr)`: amtlicher Kurzauszug (EUID, Ersteintragungsdatum/DATERST,
    Geschaeftsfuehrung samt Geburtsdatum).
  - `SearchAsync(wortlaut, ortNr)`: `SUCHEFIRMA` mit Wildcards (`*muster*`) und
    Orts-/Bezirks-/Bundesland-Einschraenkung (ORTNR 5-/3-/1-stellig, z.B. `4` = OOe).
    Antwort ist auf 1000 Treffer gedeckelt (kein Flag - exakt 1000 = vermutlich abgeschnitten).
- **`FirmenbuchCatalogSyncService`**: baut per adaptiver Praefix-Partitionierung
  (`a*`, `b*`, ... - bei 1000er-Deckel wird der Praefix verfeinert: `aa*`, `ab*`, ...)
  einen vollstaendigen Katalog aller Firmen eines Bundeslands auf und speichert ihn als
  `FirmenbuchCompany` (inkl. geloeschter/historischer Firmen, Status wird mitgefuehrt).
  Speichert inkrementell (je Suchanfrage) - Teilfortschritt ist sofort sichtbar, ein
  Abbruch verliert nichts.
- Kein Seeding (reiner Spiegel amtlicher Daten).

## Oeffentliche APIs

```
POST /api/firmenbuch/catalog/sync     TriggerFirmenbuchCatalogSyncRequest  (X-Sync-Key, fail-closed)
GET  /api/firmenbuch/catalog/status   GetFirmenbuchCatalogStatusRequest    (Zaehler je Status, LastSyncAt)
GET  /api/firmenbuch/companies        GetFirmenbuchCompaniesRequest        (Suche/Filter/Paging)
```

## Konfiguration

Abschnitt `Firmenbuch:Hvd` (`FirmenbuchHvdOptions`):

| Feld | Default | Beschreibung |
|------|---------|--------------|
| `BaseUrl` | `https://justizonline.gv.at/jop/api/at.gv.justiz.fbw/ws` | |
| `ApiKey` | `JustizOnline:IwgApiKey` | X-API-KEY; Default ist der zentrale JustizOnline-IWG-Token (PostConfigure-Fallback), hier setzbar als Override; ganz ohne Wert = Client deaktiviert |
| `TimeoutSeconds` | `30` | |
| `DelayBetweenRequestsMs` | `300` | Rate-Limit zwischen Requests (Gateway liefert bei Ueberlast HTTP 429, Resilience-Handler retryt) |
| `SyncTriggerKey` | - | Shared-Key fuer `POST /api/firmenbuch/catalog/sync` (Header `X-Sync-Key`), fail-closed ausserhalb Development. In Prod auf denselben `SYNC_TRIGGER_KEY` gemappt wie der Edikte-Sync. |

## Abhaengigkeiten

- `Heimatplatz.Api.Features.Firmenbuch.Contracts`
- `Heimatplatz.Api.Core.Data` (BaseEntity, AppDbContext)
- `Heimatplatz.Api.Shared` (DI-Konstanten, SharedKeyAuthorization)
- `Shiny.Mediator`

## Verwendung

```csharp
services.AddFirmenbuchFeature(configuration);
```

Sync ausloesen (erfordert `X-Sync-Key`):

```
curl -X POST https://api.heimatplatz.at/api/firmenbuch/catalog/sync \
  -H "X-Sync-Key: <SYNC_TRIGGER_KEY>" -H "Content-Type: application/json" \
  -d '{"OrtNr":"4"}'
```

`OrtNr` = 1-stellig Bundesland (4 = Oberoesterreich), 3-stellig Bezirk, 5-stellig Gemeinde,
leer = ganz Oesterreich (dauert entsprechend laenger).
