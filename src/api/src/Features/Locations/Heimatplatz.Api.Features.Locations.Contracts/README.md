# Heimatplatz.Api.Features.Locations.Contracts

## Zweck

Request/Response DTOs fuer das Locations Feature.

## DTOs

| DTO | Beschreibung |
|-----|-------------|
| `FederalProvinceDto` | Bundesland mit Bezirken |
| `DistrictDto` | Bezirk mit Gemeinden |
| `MunicipalityDto` | Gemeinde mit PLZ und Status |

## Mediator Requests

- `GetLocationsRequest` / `GetLocationsResponse` - Location-Hierarchie abrufen

## Abhaengigkeiten

- `Shiny.Mediator.Contracts`
