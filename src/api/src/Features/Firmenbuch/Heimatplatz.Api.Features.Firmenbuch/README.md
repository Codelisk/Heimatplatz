# Heimatplatz.Api.Features.Firmenbuch

Lokal gespeicherter **Firmenbuch-Katalog** (Firmenstammdaten, unabhaengig von einer Branche),
gespiegelt aus der **Firmenpool-API** (eigenes Repo `AIRoutine/Firmenpool` - dessen Backend
crawlt die amtliche FBW-HVD-Schnittstelle des BMJ selbst, pflegt den Bestand per Tages-Delta
und haelt zusaetzlich Auszuege, Funktionaere, GISA-Gewerbe und Jahresabschluss-Kennzahlen).
Heimatplatz crawlt die Justiz-Schnittstelle **nicht mehr selbst**.

## Zweck und Verantwortlichkeiten

- **`FirmenpoolApiClient`** (`IFirmenpoolApiClient`): duenner HTTP-Client fuer
  `GET {BaseUrl}/api/firmenbuch/companies` (seitenweise, camelCase-JSON). Der Firmenpool-Caddy
  laesst diese lesende Route nur fuer freigeschaltete IPs durch (Heimatplatz-Hetzner-Server,
  Daniels Anschluss); Sync-Trigger und Dashboard des Firmenpools bleiben gesperrt.
- **`FirmenbuchCatalogSyncService`**: zieht den kompletten Firmenpool-Katalog seitenweise ab
  und upsertet per FNR in `FirmenbuchCompany` (inkl. geloeschter/historischer Firmen, Status
  wird mitgefuehrt; `First-`/`LastSeenAt` sind die Sichtungszeitpunkte der QUELLE).
  Speichert inkrementell (je Seite) - Teilfortschritt ist sofort sichtbar, ein Abbruch
  verliert nichts. Eintraege werden nie geloescht.
- Kein Seeding (reiner Spiegel amtlicher Daten).

## Oeffentliche APIs

```
POST /api/firmenbuch/catalog/sync     TriggerFirmenbuchCatalogSyncRequest  (X-Sync-Key, fail-closed)
GET  /api/firmenbuch/catalog/status   GetFirmenbuchCatalogStatusRequest    (Zaehler je Status, LastSyncAt)
GET  /api/firmenbuch/companies        GetFirmenbuchCompaniesRequest        (Suche/Filter/Paging)
```

`TriggerFirmenbuchCatalogSyncRequest.OrtNr` ist seit dem Umstieg bedeutungslos (der raeumliche
Umfang ergibt sich aus der Quelle; der Firmenpool fuehrt derzeit Oberoesterreich) und bleibt
nur fuer Wire-Kompatibilitaet im Vertrag.

## Konfiguration

Abschnitt `Firmenbuch:Firmenpool` (`FirmenpoolOptions`):

| Feld | Default | Beschreibung |
|------|---------|--------------|
| `BaseUrl` | `https://static.91.18.104.178.clients.your-server.de` | Firmenpool-API (Uebergangsbetrieb auf dem aiconnector-Server) |
| `TimeoutSeconds` | `60` | |
| `PageSize` | `200` | Seitengroesse beim Abzug (Firmenpool deckelt bei 200) |
| `SyncTriggerKey` | Fallback `Firmenbuch:Hvd:SyncTriggerKey` | Shared-Key fuer `POST /api/firmenbuch/catalog/sync` (Header `X-Sync-Key`), fail-closed ausserhalb Development. In Prod auf denselben `SYNC_TRIGGER_KEY` gemappt wie der Edikte-Sync (env `Firmenbuch__Hvd__SyncTriggerKey`, historischer Name). |

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
  -H "X-Sync-Key: <SYNC_TRIGGER_KEY>" -H "Content-Type: application/json" -d '{}'
```
