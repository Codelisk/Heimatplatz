# Heimatplatz.Core.Startup

Zentrales DI-Setup für die Uno Platform App.

## Zweck

- Zentrale Dependency Injection Konfiguration für die App
- Registrierung von Shiny Mediator
- Bereitstellung von UnoFramework-Services
- Orchestrierung aller Feature-Registrierungen

## Öffentliche APIs

### ServiceCollectionExtensions

```csharp
services.AddAppServices();
```

Registriert:
- Shiny Mediator für CQRS-Pattern
- `UnoEventCollector` für Event-Handling
- `BaseServices` für ViewModel-Basisservices
- Auth Feature für Authentifizierung

## Abhängigkeiten

- Shiny.Mediator - Mediator Pattern
- Shiny.Mediator.Http - HTTP Extension für API-Kommunikation
- UnoFramework - BaseServices, UnoEventCollector
- Heimatplatz.Features.Auth - Auth Feature

## OpenAPI HTTP Contract Generation

Das Projekt generiert automatisch HTTP-Contracts aus der API OpenAPI-Spezifikation.

### Konfiguration (csproj)

```xml
<ItemGroup>
  <MediatorHttp Include="ApiContracts"
                Uri="http://localhost:5292/openapi/v1.json"
                Namespace="Heimatplatz.Api.Generated"
                ContractPostfix="HttpRequest"
                GenerateJsonConverters="true"
                Visible="false" />
</ItemGroup>
```

### API Base-URL (appsettings.json)

```json
{
  "Mediator": {
    "Http": {
      "Heimatplatz.Api.Generated.*": "http://localhost:5292"
    }
  }
}
```

### Verwendung

Die generierten Contracts können direkt über den Mediator verwendet werden:

```csharp
// Beispiel: Weather Forecast abrufen
var response = await mediator.Request(new GetWeatherForecastHttpRequest());
```

### Voraussetzungen

- Die API muss laufen, damit die OpenAPI-Spezifikation zur Build-Zeit verfügbar ist
- Alle API-Endpunkte müssen eine `OperationId` definiert haben

Referenz: [Shiny Mediator HTTP Extension](https://shinylib.net/mediator/extensions/http/)

## Verwendung

In `App.xaml.cs` oder Host-Builder:

```csharp
services.AddAppServices();
```

## Registrierte Features

| Feature | Methode | Beschreibung |
|---------|---------|--------------|
| Auth | `AddAuthFeature()` | JWT-basierte Authentifizierung |

## Neue Features hinzufügen

Bei neuen Features die Registrierung ergänzen:

```csharp
public static IServiceCollection AddAppServices(this IServiceCollection services)
{
    services.AddShinyMediator();
    services.AddSingleton<IEventCollector, UnoEventCollector>();
    services.AddSingleton<BaseServices>();

    // Features
    services.AddAuthFeature();
    services.Add{NeuesFeature}Feature();  // Hier hinzufügen

    return services;
}
```
