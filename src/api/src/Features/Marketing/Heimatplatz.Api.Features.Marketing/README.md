# Heimatplatz.Api.Features.Marketing

Ganzheitliches Marketing-System fuer den Intern-Bereich des Astro-Webs
(`/intern/marketing`): KI-gestuetzte Marketing-E-Mails, Kontaktdatenbank
potentieller Kunden (CRM), Versand-Historie, Posteingang mit
Rueckmeldungs-Zuordnung und Auswertungs-Kennzahlen.

## Endpoints

Alle unter `/api/admin/marketing/*`, geschuetzt per `X-Admin-Key`
(`AdminAccessGuard` aus dem Admin-Feature, fail-closed) + Caddy-IP-Sperre auf
`/api/admin*`. Fehler kommen als `Success=false` + `Error`-Text zurueck.

| Methode | Pfad | Handler |
|---------|------|---------|
| POST | `/email/generate` | `GenerateMarketingEmailHandler` |
| POST | `/email/send` | `SendMarketingEmailHandler` |
| GET | `/stats` | `GetMarketingStatsHandler` |
| GET | `/contacts` | `GetMarketingContactsHandler` |
| POST | `/contacts/save` | `SaveMarketingContactHandler` |
| DELETE | `/contacts/{Id}` | `DeleteMarketingContactHandler` |
| GET | `/contacts/detail` | `GetMarketingContactDetailHandler` |
| GET | `/emails` | `GetMarketingEmailsHandler` |
| GET | `/inbox` | `GetMarketingInboxHandler` |
| POST | `/inbox/sync` | `SyncMarketingInboxHandler` |
| POST | `/inbox/read` | `SetMarketingInboundReadHandler` |

## Datenmodell (EF, Auto-Discovery)

- `MarketingContact` - Kontaktdatenbank (E-Mail unique/normalisiert, Typ/Status-Funnel,
  Notizen, LastContactedAt/LastReplyAt). Wird beim Versand automatisch angelegt.
- `MarketingEmail` - Versand-Historie mit SMTP-`MessageId` (Reply-Threading),
  Generierungs-Stichwoertern (Auswertung) und Status `Sent`/`LoggedOnly`.
- `MarketingInboundEmail` - Rueckmeldungen aus dem Postfach; `MessageId` unique
  (Sync-Idempotenz), FKs auf Kontakt (Cascade) und beantwortete Mail (SetNull).

Migrations liegen in BEIDEN Provider-Sets (`Core.Data/Migrations` SQLite,
`Core.Data.Migrations.Postgres`), Demo-Kontakte im `MarketingSeeder` (IsDemoData).

## Ablauf

1. **Generate**: `IMarketingEmailGenerator` erstellt aus Stichwoertern (+ optionalem
   Empfaenger-Namen fuer die Anrede) einen Entwurf. Provider:
   - `Mock` (Default): Platzhalter-Text ohne KI fuer lokale Entwicklung.
   - `AiConnector`: Prompt im Workspace `projects/heimatplatz`, Section
     `sections/marketing/email` (AGENTS.md definiert Rolle, Ton und das
     JSON-Ausgabeformat `{"subject", "body"}` - geparst von
     `MarketingEmailOutputParser`).
2. **Send**: `MarketingEmailComposer` baut HTML + Plaintext und haengt die Signatur
   an; Versand ueber `IEmailSender` (Core.Email). Danach Kontakt-Upsert
   (Lead->Contacted, LastContactedAt) + Historien-Zeile mit Message-Id.
3. **Posteingang**: `MarketingInboxSyncService` ruft das Postfach per IMAP ab
   (MailKit, gleiche Zugangsdaten wie SMTP, `Email:ImapHost` leer = SmtpHost).
   Uebernommen werden NUR Antworten auf Marketing-Mails (In-Reply-To/References)
   oder Mails bekannter Kontakte - das restliche Postfach bleibt privat.
   Auto-Sync beim Oeffnen der Posteingang-Seite (5-Minuten-Drossel), manueller
   Sync ueber `/inbox/sync`. Eingehende Antworten setzen den Kontakt-Status
   Lead/Contacted -> Replied.
4. **Auswertung**: `/stats` liefert Kontakt-Funnel, Versand-/Antwort-Volumen
   (30 Tage), ungelesene Rueckmeldungen und die Antwortquote.

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
`Heimatplatz.Api.Core.AiConnectorClient` (`Mediator:Http:...`, `AiConnector:ApiKey`),
der AiConnector-Timeout (5 Minuten) aus dessen partial-class-Override.
Der Posteingang nutzt die `Email:*`-Konfiguration (Core.Email) - kein eigenes Env noetig.

## Abhaengigkeiten

- `Heimatplatz.Api.Features.Marketing.Contracts` (Requests/DTOs/Enums)
- `Heimatplatz.Api.Features.Admin` (`IAdminAccessGuard`)
- `Heimatplatz.Api.Features.Legal.Contracts` (`GetImprintRequest` fuer die Signatur)
- `Heimatplatz.Api.Core.Data` (+Seeding) (AppDbContext, Entities, Seeder)
- `Heimatplatz.Api.Core.Email` (`IEmailSender`, `EmailOptions`, MailKit/IMAP)
- `Heimatplatz.Api.Core.AiConnectorClient` (generierter `RunPromptHttpRequest`-Client)
