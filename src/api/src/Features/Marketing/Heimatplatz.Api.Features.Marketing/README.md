# Heimatplatz.Api.Features.Marketing

Marketing-Funktionen fuer den Intern-Bereich des Astro-Webs (`/intern/marketing`).
Erster Baustein: KI-gestuetzte Marketing-E-Mails - Stichwoerter rein, Entwurf
(Betreff + Text) raus, im Web nachbearbeiten, dann Versand von `info@heimatplatz.at`
mit automatisch angehaengter Signatur.

## Endpoints

| Methode | Pfad | Handler |
|---------|------|---------|
| POST | `/api/admin/marketing/email/generate` | `GenerateMarketingEmailHandler` |
| POST | `/api/admin/marketing/email/send` | `SendMarketingEmailHandler` |

Beide Endpoints liegen bewusst unter `/api/admin/*`: gleicher `X-Admin-Key`-Schutz
(`AdminAccessGuard` aus dem Admin-Feature, fail-closed) und gleiche Caddy-IP-Sperre
auf `/api/admin*` (deploy/hetzner/Caddyfile). Fehler kommen als `Success=false` +
`Error`-Text zurueck, damit der Intern-Bereich die Ursache anzeigen kann.

## Ablauf

1. **Generate**: `IMarketingEmailGenerator` erstellt aus Stichwoertern (+ optionalem
   Empfaenger-Namen fuer die Anrede) einen Entwurf. Provider:
   - `Mock` (Default): Platzhalter-Text ohne KI fuer lokale Entwicklung.
   - `AiConnector`: Prompt im Workspace `projects/heimatplatz`, Section
     `sections/marketing/email` (AGENTS.md definiert Rolle, Ton und das
     JSON-Ausgabeformat `{"subject", "body"}` - geparst von
     `MarketingEmailOutputParser`).
2. **Send**: `MarketingEmailComposer` baut HTML + Plaintext und haengt die Signatur
   an; Versand ueber `IEmailSender` (Core.Email, SMTP oder Logging-Fallback).
   `SmtpConfigured=false` in der Response heisst: Mail wurde nur geloggt.

## Signatur

Quelle ist das aktive Impressum (`GetImprintRequest` -> `LegalSettings`), dieselben
Kontaktdaten wie `/impressum` - kein zweiter Pflegeort. Telefon erscheint nur, wenn
im Impressum gepflegt. Layout im Stil der Auth-Mails (Arial, Markenrot `#b3261e`).

## Konfiguration

```json
{
  "Marketing": {
    "Provider": "Mock",
    "AiConnector": {
      "WorkspaceId": "projects/heimatplatz",
      "SectionPath": "sections/marketing/email"
    }
  }
}
```

Produktion setzt `Marketing__Provider=AiConnector` (deploy/hetzner/docker-compose.yml);
Basis-URL/API-Key des AiConnectors kommen zentral aus dem
`Heimatplatz.Api.Core.AiConnectorClient` (`Mediator:Http:...`, `AiConnector:ApiKey`).

## Abhaengigkeiten

- `Heimatplatz.Api.Features.Marketing.Contracts` (Requests/Responses)
- `Heimatplatz.Api.Features.Admin` (`IAdminAccessGuard`)
- `Heimatplatz.Api.Features.Legal.Contracts` (`GetImprintRequest` fuer die Signatur)
- `Heimatplatz.Api.Core.Email` (`IEmailSender`, `EmailOptions`)
- `Heimatplatz.Api.Core.AiConnectorClient` (generierter `RunPromptHttpRequest`-Client)
