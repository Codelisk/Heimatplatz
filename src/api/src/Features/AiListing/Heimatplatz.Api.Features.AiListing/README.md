# Heimatplatz.Api.Features.AiListing

KI-Unterstuetzung fuer die Inseratserstellung: Medien-Upload fuer den Wizard und die
**asynchrone Generierung der Inserats-Beschreibung** aus den manuell erfassten Eckdaten,
den Stichwoertern des Verkaeufers und den Foto-URLs.

Die frueher hier beheimatete KI-Extraktion der Inseratsfelder (ListingAnalysis-Flow,
`POST/GET /api/ai-listings`) wurde mit dem Umbau auf den manuellen Wizard entfernt —
der Nutzer erfasst Eckdaten/Lage/Preis selbst, nur die Beschreibung kann generiert werden.

**Bewusst NICHT per KI befuellt:** Eckdaten, Preis, Adresse, Gemeinde, Verkaeufer-/Kontaktdaten.

## Ablauf

1. `POST /api/ai-listings/media` — Fotos als Base64 hochladen (idealerweise eine Datei
   pro Request), gespeichert unter `wwwroot/uploads/listings/`. (Video-Support besteht
   fuer Altbestand weiter, der Wizard laedt keine Videos mehr hoch.)
2. Das PropertyDrafts-Feature fordert die Beschreibung ueber den **in-process**
   Mediator-Request `GenerateListingDescriptionRequest` an (kein HTTP-Endpoint; laeuft
   im TickerQ-Hintergrund-Job `generate-draft-description`, siehe PropertyDrafts-README).
3. `GenerateListingDescriptionHandler` delegiert an den konfigurierten Provider
   (`IListingDescriptionService`), der einen Fliesstext im konfigurierten Wortbereich
   liefert.

Der Media-Endpoint erfordert die Rolle `Seller`.

## Beschreibungs-Provider (`IListingDescriptionService`)

| Provider | Klasse | Verwendung |
|----------|--------|------------|
| `Mock` (Default) | `MockListingDescriptionService` | Dev: Template-Beschreibung aus Eckdaten + Stichwoertern, konfigurierbare Verzoegerung (`Description:MockDelaySeconds`) macht den asynchronen Job-Flow sichtbar |
| `AiConnector` | `AiConnectorListingDescriptionService` | Produktion: ruft den externen AiConnector-Backend-Service ueber den generierten Shiny.Mediator-OpenAPI-Client (`Heimatplatz.Api.Core.AiConnectorClient`, `RunPromptHttpRequest`) auf. Der Prompt laeuft im Workspace `projects/heimatplatz`. Fotos werden als oeffentliche URLs mitgegeben (der Workspace-Agent kann sie abrufen), die Bilddaten selbst werden nicht uebertragen |

## Konfiguration (`AiListing` Section)

```json
{
  "AiListing": {
    "Provider": "AiConnector",
    "MaxImages": 20,
    "MaxVideos": 3,
    "MaxVideoSizeMb": 60,
    "AiConnector": {
      "WorkspaceId": "projects/heimatplatz",
      "Model": null
    },
    "Description": {
      "MinWords": 100,
      "MaxWords": 160,
      "MockDelaySeconds": 8
    }
  },
  "AiConnector": {
    "ApiKey": ""
  },
  "Mediator": {
    "Http": {
      "Heimatplatz.Api.Core.AiConnectorClient.Generated.*": "https://ai.danielhufnagl.at"
    }
  }
}
```

`Description:MinWords`/`MaxWords` definieren den **fix im Backend vorgegebenen Wortbereich**
der generierten Beschreibung. Default ist `Provider: "Mock"` (siehe `appsettings.json`),
damit der Flow lokal ohne KI funktioniert. Basis-URL und API-Key des AiConnector-Backends
werden zentral im `Heimatplatz.Api.Core.AiConnectorClient` konfiguriert (siehe dessen
README). Der `AiConnector.ApiKey` wird NICHT committet, sondern auf dem Server per
Env-Variable `AiConnector__ApiKey` gesetzt. Die Server-IP der API muss zusaetzlich in der
Caddy-Whitelist des AiConnectors (`/etc/caddy/aiconnector.env`, `AICONNECTOR_HOME_IP`)
eingetragen sein.

## Abhaengigkeiten

- `Heimatplatz.Api.Features.AiListing.Contracts` — Request/Response DTOs
- `Heimatplatz.Api.Core.AiConnectorClient` — generierter Shiny.Mediator-HTTP-Client fuer den `AiConnector`-Provider
- `Heimatplatz.Api.Core.Data` — (transitiv, keine eigenen Entities mehr)
- `Heimatplatz.Api.Shared` — `ApiService` DI-Konstanten, `AuthorizationPolicies`
