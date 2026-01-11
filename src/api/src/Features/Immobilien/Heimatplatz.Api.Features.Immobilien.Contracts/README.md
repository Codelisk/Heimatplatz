# Heimatplatz.Api.Features.Immobilien.Contracts

Contracts-Projekt fuer das Immobilien-Feature. Enthaelt alle DTOs, Enums und Mediator-Request/Response-Definitionen.

## Inhalt

### Enums
- `ImmobilienTyp` - Klassifikation: Haus, Grundstueck, Wohnung
- `ImmobilienStatus` - Status: Aktiv, Reserviert, Verkauft, Inaktiv
- `ImmobilienSortierung` - Sortieroptionen fuer Listen
- `SortierRichtung` - Aufsteigend/Absteigend

### Mediator Requests
- `GetImmobilienRequest` - Paginierte Liste mit Filtern
- `GetImmobilieByIdRequest` - Einzelne Immobilie mit Details
- `GetImmobilienTypenRequest` - Verfuegbare Typen fuer Filter
- `GetImmobilienAnzahlRequest` - Objekt-Zaehler

## Abhaengigkeiten
- Shiny.Mediator (IRequest<T>)
