# Heimatplatz.Api.Features.Properties

Hauptprojekt fuer das Properties (Immobilien) Feature.

## Inhalt

### Data
- `Property` Entity - Immobilien-Datenmodell
- `PropertyConfiguration` - EF Core Konfiguration
- `PropertySeeder` - Beispieldaten fuer Entwicklung

### Handlers
- `GetPropertiesHandler` - Gefilterte Immobilien-Liste
- `GetPropertyByIdHandler` - Einzelne Immobilie

## Verwendung

```csharp
// In ServiceCollectionExtensions.cs
services.AddPropertiesFeature();
```
