# Heimatplatz.Api.Features.Marketing.Contracts

Request/Response-Contracts des Marketing-Features (Intern-Bereich): KI-gestuetzte
Erstellung und Versand von Marketing-E-Mails von `info@heimatplatz.at`.

## Contracts

| Request | Response | Zweck |
|---------|----------|-------|
| `GenerateMarketingEmailRequest` | `GenerateMarketingEmailResponse` | E-Mail-Entwurf (Betreff + Text) aus Stichwoertern generieren (Provider Mock/AiConnector) |
| `SendMarketingEmailRequest` | `SendMarketingEmailResponse` | Entwurf (ggf. nachbearbeitet) mit automatischer Signatur versenden |

Beide Requests werden vom `Heimatplatz.Api.Features.Marketing`-Projekt als
`POST /api/admin/marketing/email/generate` bzw. `POST /api/admin/marketing/email/send`
gemappt und sind ueber den `X-Admin-Key`-Header geschuetzt (siehe Admin-Feature).

## Design-Hinweise

- Alle Parameter liegen im Body (kein Route-Parameter), damit das Binding der
  generierten Shiny.Mediator-Endpoints eindeutig ist.
- Die Empfaenger-E-Mail-Adresse wird nur beim Versand verwendet und bewusst nicht
  an die KI-Generierung uebergeben.
- Fehler werden als `Success=false` + `Error`-Text zurueckgegeben (Anzeige im
  Intern-Bereich), nicht als HTTP-Fehlercodes.

## Abhaengigkeiten

- `Shiny.Mediator.Contracts` (IRequest)
