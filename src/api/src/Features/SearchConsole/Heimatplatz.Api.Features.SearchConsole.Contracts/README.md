# Heimatplatz.Api.Features.SearchConsole.Contracts

Request/Response-DTOs (Shiny-Mediator-Contracts) fuer das SearchConsole-Feature - Suchperformance-Kennzahlen aus der Google Search Console fuer den Intern-Bereich (`/intern/analytics`).

## Requests

| Request | Endpoint | Zweck |
|---------|----------|-------|
| `GetSearchConsoleSummaryRequest` | `GET /api/admin/search-console/summary` | Klicks/Impressionen/CTR/Position der letzten 28 Tage + Top-10-Suchbegriffe/-Seiten |

## Sicherheit

Verlangt den Shared-Key-Header `X-Admin-Key` (siehe `Heimatplatz.Api.Features.Admin`) - gleiches Muster wie alle `/api/admin`-Endpoints.

## Abhaengigkeiten

- `Shiny.Mediator.Contracts`
