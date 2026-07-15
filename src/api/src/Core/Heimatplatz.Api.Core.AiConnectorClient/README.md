# Heimatplatz.Api.Core.AiConnectorClient

Generierter OpenAPI-HTTP-Client (Shiny.Mediator `MediatorHttp`) aus `src/api/openapi/AiConnector.json`
fuer den externen AiConnector-Backend-Service (`ai.danielhufnagl.at`).

## Zweck

- Stellt alle AiConnector-Endpoints (`/api/prompt`, `/api/workspaces`, `/api/login`, ...) als
  Mediator-HTTP-Requests bereit (Namespace `Heimatplatz.Api.Core.AiConnectorClient.Generated`, Postfix `HttpRequest`).
- **Eigenes Projekt**, damit der `MediatorHttp`-Source-Generator nicht mit anderen Generatoren
  (z.B. Shiny DI `[Service]`) im selben Kompilat kollidiert — gleiches Muster wie
  `Heimatplatz.Maui.ApiClient`.
- `AiConnectorApiKeyDecorator` (`Http/`) haengt den `X-Api-Key`-Header ausschliesslich an
  Requests aus dem `.Generated`-Namespace dieses Clients, damit andere generierte HTTP-Clients
  (z.B. der eigene Heimatplatz-API-Contract) den Key nicht mitbekommen.

## Verwendung

```csharp
services.AddAiConnectorClient(configuration); // registriert AddGeneratedOpenApiClient() + Decorator

var response = await mediator.Request(new RunPromptHttpRequest
{
    Body = new PromptRequest { Prompt = "...", WorkspaceId = "projects/heimatplatz" }
});
```

## Konfiguration

```json
{
  "Mediator": {
    "Http": {
      "Heimatplatz.Api.Core.AiConnectorClient.Generated.*": "https://ai.danielhufnagl.at"
    }
  },
  "AiConnector": {
    "ApiKey": ""
  }
}
```

`AiConnector:ApiKey` wird NICHT committet, sondern per Env-Variable `AiConnector__ApiKey` gesetzt
(siehe `deploy/hetzner/docker-compose.yml`, `AICONNECTOR_API_KEY`). Die Server-IP muss zusaetzlich
in der Caddy-Whitelist des AiConnectors (`/etc/caddy/aiconnector.env`, `AICONNECTOR_HOME_IP`)
eingetragen sein.

## Aktualisieren des Specs

`src/api/openapi/AiConnector.json` ist ein einmalig heruntergeladenes Snapshot
(`curl https://ai.danielhufnagl.at/openapi.json`), nicht dynamisch zur Build-Zeit geholt — das
haelt Builds reproduzierbar/offline-faehig. Bei Aenderungen am AiConnector-Backend die Datei
manuell neu herunterladen und committen.

## Abhaengigkeiten

- `Shiny.Mediator` (Source Generator + Runtime)
