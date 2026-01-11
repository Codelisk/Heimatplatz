# Heimatplatz.Api.Features.Immobilien

Hauptprojekt fuer das Immobilien-Feature. Enthaelt Entities, Datenbankonfigurationen, Handler und Seeding.

## Inhalt

### Data/Entities
- `Immobilie` - Hauptentity fuer Immobilien/Grundstuecke
- `ImmobilieBild` - Bilder zu einer Immobilie

### Data/Configurations
- `ImmobilieConfiguration` - EF Core Konfiguration mit Indizes
- `ImmobilieBildConfiguration` - EF Core Konfiguration fuer Bilder

### Handlers (Shiny Mediator)
- `GetImmobilienHandler` - Paginierte Liste mit Filtern
- `GetImmobilieByIdHandler` - Einzelne Immobilie
- `GetImmobilienTypenHandler` - Verfuegbare Typen
- `GetImmobilienAnzahlHandler` - Objekt-Zaehler

### Seeding
- `ImmobilienSeeder` - Testdaten aus Oberoesterreich

## Abhaengigkeiten
- Heimatplatz.Api.Features.Immobilien.Contracts
- Heimatplatz.Api.Core.Data (BaseEntity, AppDbContext)
- Heimatplatz.Api.Core.Data.Seeding (ISeeder)
- Heimatplatz.Api.Shared (ApiService)

## Registrierung

```csharp
services.AddImmobilienFeature();
```
