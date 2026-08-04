# Heimatplatz.Api.Features.Partners

Partner-Verwaltung fuer die oeffentliche `/partner/`-Seite (Makler-Partner wie Immobär)
und die Intern-Pflege `/intern/partner/`. Konzept: `docs/partner-seite-konzept.md`.

## Verantwortlichkeiten

- `Partner`-Entity + EF-Konfiguration (Tabelle `Partners`)
- Oeffentliche Partnerliste mit Live-Inseratszahl
- Admin-CRUD inkl. Logo-Upload (X-Admin-Key, fail-closed)
- Demo-Seeder fuer Entwicklung/Test

## Endpoints

| Methode | Pfad | Zweck | Zugriff |
|---|---|---|---|
| GET | `/api/partners` | Sichtbare Partner fuer die Web-Seite | oeffentlich |
| GET | `/api/admin/partners` | Alle Partner (auch ausgeblendete) | X-Admin-Key |
| POST | `/api/admin/partners/save` | Anlegen / vollstaendiges Ersetzen | X-Admin-Key |
| POST | `/api/admin/partners/delete` | Endgueltig loeschen (inkl. Logo-Datei) | X-Admin-Key |
| POST | `/api/admin/partners/logo` | Logo-Upload (Base64, Bild-Pipeline) | X-Admin-Key |

Fachliche Fehler kommen als `Success=false` + `Error` (HTTP 200), damit die
Intern-Seite konkrete Meldungen zeigen kann.

## Live-Inseratszahl

`PartnerListingCounts` zaehlt `Property`-Zeilen je `SourceName` (z.B. `immobaer.at`,
siehe `OpenImmoImport`-Feed-Konfiguration) mit derselben Sichtbarkeitsregel wie die
oeffentliche Suche (`!IsHidden`). `Partner.SourceName == null` bedeutet: keine
automatische Zaehlung, die Web-Seite blendet die Zahl dann aus.

## Wichtige Entscheidungen

- **IsVisible ohne DB-Default:** CLR-Init `true` + `HasDefaultValue(true)` wuerde
  wegen der EF-bool-Sentinel-Falle ein explizites `false` beim Insert verschlucken.
  Der Wert wird deshalb immer mitgeschrieben.
- **Logos selbst hosten:** Upload ueber `IPropertyImageService` (Original +
  Display-Variante). Kein Hotlink auf Partner-Domains - die Web-CSP blockt fremde
  `img-src`, und Partner-Server sollen keine Besucher-IPs sehen.
- **Kein Prod-Seeder:** Partner sind echte Vertragsdaten. Der Seeder ist
  `IsDemoData=true` und liefert fiktive Eintraege fuer Dev/Test.

## Abhaengigkeiten

- `Heimatplatz.Api.Core.Data` / `Core.Data.Seeding` / `Api.Shared`
- `Heimatplatz.Api.Features.Admin` (`IAdminAccessGuard`)
- `Heimatplatz.Api.Features.Properties` (`Property`-Entity fuer die Zaehlung,
  `IPropertyImageService` fuer den Logo-Upload)
