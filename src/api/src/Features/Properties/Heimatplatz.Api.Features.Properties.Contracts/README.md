# Heimatplatz.Api.Features.Properties.Contracts

Contracts und DTOs fuer das Properties (Immobilien) Feature.

## Inhalt

### Enums
- `PropertyType` - Art der Immobilie (Haus, Grundstueck, Zwangsversteigerung)
- `SellerType` - Art des Anbieters (Privat, Makler)

### DTOs
- `PropertyDto` - Vollstaendige Immobilien-Daten
- `PropertyListItemDto` - Kompakte Daten fuer Listen

### Requests
- `GetPropertiesRequest` - Immobilien-Liste mit Filtern abrufen
- `GetPropertyByIdRequest` - Einzelne Immobilie abrufen
