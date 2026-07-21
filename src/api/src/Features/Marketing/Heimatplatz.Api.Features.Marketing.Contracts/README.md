# Heimatplatz.Api.Features.Marketing.Contracts

Request/Response-Contracts, DTOs und Enums des Marketing-Features (Intern-Bereich):
KI-gestuetzte Marketing-E-Mails, Kontaktdatenbank (CRM), Versand-Historie,
Posteingang und Auswertung.

## Contracts

| Request | Zweck |
|---------|-------|
| `GenerateMarketingEmailRequest` | E-Mail-Entwurf (Betreff + Text) aus Stichwoertern generieren |
| `GetMarketingEmailSignatureRequest` | Signatur-Vorschau fuer selbst geschriebene E-Mails (ohne Generierung) |
| `SendMarketingEmailRequest` | Entwurf versenden (selbst geschrieben oder generiert, optional CC/BCC); legt Kontakt an und speichert Historie |
| `GetMarketingStatsRequest` | Dashboard-Kennzahlen (Funnel, Volumen, Antwortquote) |
| `GetMarketingContactsRequest` | Kontaktliste (Suche/Filter/Paging) |
| `SaveMarketingContactRequest` | Kontakt anlegen/bearbeiten (Upsert) |
| `DeleteMarketingContactRequest` | Kontakt samt Historie loeschen (DSGVO) |
| `GetMarketingContactDetailRequest` | Kontakt-Detail + Timeline |
| `GetMarketingEmailsRequest` | Versand-Historie |
| `GetMarketingInboxRequest` | Posteingang (mit gedrosseltem Auto-Sync) |
| `SyncMarketingInboxRequest` | Manueller Postfach-Abruf |
| `SetMarketingInboundReadRequest` | Gelesen-Markierung |

DTOs: `MarketingContactDto`, `MarketingEmailDto`, `MarketingInboundEmailDto`.
Enums (`MarketingEnums.cs`): `MarketingContactType`, `MarketingContactStatus`,
`MarketingEmailStatus` - serialisiert per globalem JsonStringEnumConverter als
Enum-NAMEN-Strings; das Web vergleicht Strings, nie Zahlen.

## Design-Hinweise

- Schreibende Requests liegen komplett im Body (kein Route-Parameter), damit das
  Binding der generierten Shiny.Mediator-Endpoints eindeutig ist (Ausnahme:
  `DELETE /contacts/{Id}` nach dem Muster des Admin-Features).
- Listen-Filter (Status/Typ) sind bewusst `string` statt Enum, damit das
  Query-Binding robust bleibt (leer = kein Filter).
- Fehler werden als `Success=false` + `Error`-Text zurueckgegeben (Anzeige im
  Intern-Bereich), nicht als HTTP-Fehlercodes.

## Abhaengigkeiten

- `Shiny.Mediator.Contracts` (IRequest)
