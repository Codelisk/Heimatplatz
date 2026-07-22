# Heimatplatz.Api.Features.Firmenbuch.Contracts

Request/Response-DTOs (Shiny.Mediator-Contracts) fuer das Firmenbuch-Feature.

## Inhalte

- `TriggerFirmenbuchCatalogSyncRequest` / `...Response` - Katalog-Sync ausloesen
  (`OrtNr`: 1-stellig Bundesland / 3-stellig Bezirk / 5-stellig Gemeinde / leer = alle)
- `GetFirmenbuchCatalogStatusRequest` / `...Response` - Bestandszaehler je Status + LastSyncAt
- `GetFirmenbuchCompaniesRequest` / `...Response` - Katalog abfragen (Suche, Sitz-, Status-,
  Rechtsform-Filter, Paging) mit `FirmenbuchCompanyDto`

## Abhaengigkeiten

- `Shiny.Mediator.Contracts`
