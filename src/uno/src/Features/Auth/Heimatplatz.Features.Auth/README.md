# Heimatplatz.Features.Auth

Auth Feature der Uno App - Benutzerregistrierung und Authentifizierung.

## Inhalt

### Presentation
- `RegisterPage`: Registrierungsseite mit responsivem Layout (Desktop/Mobile)
- `RegisterViewModel`: ViewModel fuer die Registrierung

### Configuration
- `ServiceCollectionExtensions`: Feature-Registrierung

## API-Kommunikation

Die Kommunikation mit der API erfolgt ueber den automatisch generierten ApiClient (`RegisterHttpRequest`), der via Shiny.Mediator aufgerufen wird.

## Verwendung

```csharp
services.AddAuthFeature();
```
