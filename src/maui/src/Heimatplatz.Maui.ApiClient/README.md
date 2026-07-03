# Heimatplatz.Maui.ApiClient

Generierter OpenAPI-HTTP-Client (Shiny.Mediator `MediatorHttp`) aus `src/api/openapi/Heimatplatz.Api.json`.

## Zweck

- Stellt alle API-Contracts als Mediator-HTTP-Requests bereit (Namespace `Heimatplatz.Maui.ApiClient.Generated`, Postfix `HttpRequest`).
- **Eigenes Projekt, weil Source-Generatoren die Ausgaben anderer Generatoren nicht sehen:** Die MAUI-App nutzt die generierten DTOs in `[ObservableProperty]`/`[RelayCommand]`-Signaturen (CommunityToolkit.Mvvm Source-Gen) — das funktioniert nur, wenn die Typen als Assembly-Referenz vorliegen (gleicher Grund wie `Heimatplatz.Core.ApiClient` in der Uno-App).

## Verwendung

```csharp
builder.Services.AddApiClientFeature(); // registriert AddGeneratedOpenApiClient()
var response = await mediator.Request(new GetPropertiesHttpRequest { ... });
```

Basis-URL via Konfiguration `Mediator:Http:Heimatplatz.Maui.ApiClient.Generated.*`.

## Abhängigkeiten

- `Shiny.Mediator` (Source Generator + Runtime)
