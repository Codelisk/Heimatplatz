# Heimatplatz.Api.Features.Locations

## Zweck

Verwaltung der oesterreichischen Verwaltungshierarchie: Bundesland -> Bezirk -> Gemeinde.

## Services

- **LocationSeeder**: Importiert Location-Daten von der OpenPLZ API beim ersten Start
- **GetLocationsHandler**: Stellt die Location-Hierarchie ueber REST API bereit

## Datenmodell

| Entity | Beschreibung |
|--------|-------------|
| `FederalProvince` | Bundesland (z.B. Oberoesterreich) |
| `District` | Politischer Bezirk (z.B. Linz-Land) |
| `Municipality` | Gemeinde (z.B. Traun, Leonding) |

## API Endpoints

- `GET /api/locations` - Komplette Hierarchie (optional gefiltert nach Bundesland)

## Datenquelle

[OpenPLZ API](https://openplzapi.org) - Freie REST API fuer oesterreichische Verwaltungsdaten.

## Abhaengigkeiten

- `Heimatplatz.Api.Core.Data` - DbContext und BaseEntity
- `Heimatplatz.Api.Core.Data.Seeding` - Seeder-Infrastruktur
- `Heimatplatz.Api.Features.Locations.Contracts` - DTOs
