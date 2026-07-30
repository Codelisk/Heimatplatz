# Heimatplatz.Api.Features.Firmenbuch

Duenner HTTP-Client fuer die **Firmenpool-API** (eigenes Repo `AIRoutine/Firmenpool` - dessen
Backend crawlt die amtliche FBW-HVD-Schnittstelle des BMJ, pflegt den Bestand per Tages-Delta
und haelt Auszuege, Funktionaere, GISA-Gewerbe und Jahresabschluss-Kennzahlen).

**Heimatplatz speichert selbst keine Firmenbuch-Daten mehr.** Der Marketing-Lead-Pool fragt
live beim Firmenpool an; erst wenn eine Firma als Kontakt uebernommen wird, entsteht ein
Datensatz in der Heimatplatz-Datenbank (`MarketingContact`, Schluessel `FirmenbuchFnr`).
Dieses Projekt stellt nur den Client bereit - Endpoints und Fachlogik liegen im
Marketing-Feature.

## Oeffentliche Services

- **`IFirmenpoolApiClient`**
  - `GetCompaniesAsync(FirmenpoolCompanyQuery)`: Firmenliste mit serverseitigen Filtern
    (SearchText, Sitz kommasepariert/Teilstring, Status "aufrecht", NameContainsAny-Schlagworte,
    ExcludeFnrs) und exakter Trefferzahl/Paging (PageSize max. 200, Seiten 1-basiert).
  - `GetCompanyDetailAsync(fnr)`: voller Datensatz inkl. Adresse, Funktionaeren, Gewerben und
    Abschluss-Anzahl; `null` bei unbekannter FNR.

FNRs sind kanonisch ohne fuehrende Nullen ("91180p"); die Quelle normalisiert Eingaben selbst.

## Konfiguration

Abschnitt `Firmenbuch:Firmenpool` (`FirmenpoolOptions`):

| Feld | Default | Beschreibung |
|------|---------|--------------|
| `BaseUrl` | `https://static.91.18.104.178.clients.your-server.de` | Firmenpool-API (Uebergangsbetrieb auf dem aiconnector-Server); dessen Caddy laesst die lesenden Firmendaten-Routen nur fuer freigeschaltete IPs durch (Heimatplatz-Hetzner, Daniels Anschluss) |
| `TimeoutSeconds` | `60` | |

## Abhaengigkeiten

- `Microsoft.Extensions.Http.Resilience` (Standard-Resilience um den HttpClient)

## Verwendung

```csharp
services.AddFirmenbuchFeature(configuration);
```
