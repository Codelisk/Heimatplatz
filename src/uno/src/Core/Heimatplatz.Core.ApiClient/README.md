# Heimatplatz.Core.ApiClient

API-Client-Bibliothek für die Kommunikation mit dem Backend via Shiny.Mediator HTTP Extension.

## Zweck

Dieses Projekt generiert automatisch HTTP-Contracts aus der OpenAPI-Spezifikation des Backends und stellt diese für alle Uno-Features bereit.

## Funktionsweise

### OpenAPI Contract Generation

Die Contracts werden zur Build-Zeit aus der API-Spezifikation generiert:

```xml
<MediatorHttp Include="ApiContracts"
              Uri="http://localhost:5292/openapi/v1.json"
              Namespace="Heimatplatz.Core.ApiClient.Generated"
              ContractPostfix="HttpRequest"
              GenerateJsonConverters="true"
              Visible="false" />
```

### Verwendung in Features

```csharp
// Inject IMediator
public class MyViewModel(IMediator mediator)
{
    public async Task LoadData()
    {
        var response = await mediator.Request(new GetDataHttpRequest());
    }
}
```

### Handler-Registrierung

**Wichtig:** Die generierten HTTP-Handler müssen explizit registriert werden:

```csharp
// In ServiceCollectionExtensions.cs
public static IServiceCollection AddApiClientFeature(this IServiceCollection services)
{
    services.AddGeneratedOpenApiClient(); // Generiert vom Source Generator
    return services;
}
```

Ohne diesen Aufruf: `No request handler found for XxxHttpRequest`

### Konfiguration

Base-URL in `appsettings.json`:

```json
{
  "Mediator": {
    "Http": {
      "Heimatplatz.Core.ApiClient.Generated.*": "http://localhost:5292"
    }
  }
}
```

## Abhängigkeiten

- `Shiny.Mediator` - Mediator Pattern Implementation
- `Shiny.Mediator.Http` - HTTP Extension für automatische Request-Handling

## Referenzen

- [Shiny.Mediator HTTP Extension](https://shinylib.net/mediator/extensions/http/)
- [Shiny.Mediator GitHub](https://github.com/shinyorg/mediator)
