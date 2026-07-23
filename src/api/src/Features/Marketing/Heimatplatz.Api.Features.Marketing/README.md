# Heimatplatz.Api.Features.Marketing

Ganzheitliches Marketing-System fuer den Intern-Bereich des Astro-Webs
(`/intern/marketing`): Firmenpool (Lead-Akquise aus dem Firmenbuch-Katalog),
KI- oder vorlagengestuetzte Marketing-E-Mails, Kontaktdatenbank potentieller
Kunden (CRM) mit Aktivitaeten-Historie und Wiedervorlagen, Versand-Historie,
Posteingang mit Rueckmeldungs-Zuordnung und Auswertungs-Kennzahlen.

## Akquise-Ablauf (Firmenpool -> Kontakt -> Versand)

1. **Firmenpool** (`/lead-pool`): aufrechte Firmen aus dem Firmenbuch-Katalog
   (`Heimatplatz.Api.Features.Firmenbuch`), deren Name auf die Immobilienbranche
   hindeutet (Schlagwortliste `Marketing:LeadPool:NameKeywords`, ohne Deploy
   nachschaerfbar - das Firmenbuch fuehrt keine Branche). Auswahl -> `/lead-pool/add`
   legt Kontakte mit Status `ToContact` an (idempotent ueber `FirmenbuchFnr`).
2. **Telefonat**: Der Kontakt hat zunaechst KEINE E-Mail (Firmenbuch fuehrt keine
   Kontaktdaten). Ueber `/contacts/activity` werden Anruf, Notiz, Wiedervorlage
   und Statuswechsel in einem Schritt festgehalten.
3. **E-Mail** nach dem Gespraech: Adresse am Kontakt eintragen, per Vorlage
   (`/templates/render`, Platzhalter aus dem Kontakt) oder KI einen Entwurf
   erstellen und mit `ContactId` versenden - so wird der bestehende Kontakt
   fortgeschrieben statt eine Dublette anzulegen.

## Endpoints

Alle unter `/api/admin/marketing/*`, geschuetzt per `X-Admin-Key`
(`AdminAccessGuard` aus dem Admin-Feature, fail-closed) + Caddy-IP-Sperre auf
`/api/admin*`. Fehler kommen als `Success=false` + `Error`-Text zurueck.

Ausnahme: `POST /api/marketing/broker-lead` (`SubmitBrokerLeadHandler`) ist
OEFFENTLICH (anonym, bewusst NICHT unter `/api/admin`) - das Anfrage-Formular
der `/makler/`-Seite des Webs. Upsert in die Kontaktdatenbank (Typ Broker,
Status Interested, Quelle "Makler-Anfrage", Anfrage als Notiz; gepflegte Felder
werden nie ueberschrieben) + Benachrichtigungs-Mail ans Team-Postfach
(`Email:FromAddress`). Spam-Schutz: Honeypot-Feld `Fax` (still verworfen) und
enges Rate-Limit (5/min pro IP, Program.cs).

| Methode | Pfad | Handler |
|---------|------|---------|
| POST | `/email/generate` | `GenerateMarketingEmailHandler` |
| GET | `/email/signature` | `GetMarketingEmailSignatureHandler` |
| POST | `/email/send` | `SendMarketingEmailHandler` |
| GET | `/stats` | `GetMarketingStatsHandler` |
| GET | `/contacts` | `GetMarketingContactsHandler` (Filter `dueOnly` = faellige Wiedervorlagen) |
| POST | `/contacts/save` | `SaveMarketingContactHandler` |
| DELETE | `/contacts/{Id}` | `DeleteMarketingContactHandler` |
| GET | `/contacts/detail` | `GetMarketingContactDetailHandler` (inkl. Aktivitaeten-Timeline) |
| POST | `/contacts/activity` | `LogMarketingActivityHandler` (Anruf/Notiz/Termin + Status + Wiedervorlage) |
| GET | `/lead-pool` | `GetMarketingLeadPoolHandler` (Firmenbuch-Immobilienfirmen) |
| POST | `/lead-pool/add` | `AddMarketingLeadsHandler` (uebernehmen als `ToContact`) |
| GET | `/templates` | `GetMarketingTemplatesHandler` |
| POST | `/templates/save` | `SaveMarketingTemplateHandler` |
| POST | `/templates/render` | `RenderMarketingTemplateHandler` (Platzhalter aus Kontakt) |
| DELETE | `/templates/{Id}` | `DeleteMarketingTemplateHandler` |
| GET | `/emails` | `GetMarketingEmailsHandler` |
| GET | `/inbox` | `GetMarketingInboxHandler` |
| POST | `/inbox/sync` | `SyncMarketingInboxHandler` |
| POST | `/inbox/read` | `SetMarketingInboundReadHandler` |

## Datenmodell (EF, Auto-Discovery)

- `MarketingContact` - Kontaktdatenbank. **E-Mail optional** (Firmenpool-Kontakte
  entstehen ohne Adresse), aber unique/normalisiert sofern gesetzt (partieller Index
  `"Email" IS NOT NULL`). Typ/Status-Funnel (Status inkl. `ToContact`/`FollowUp`),
  `City`, `FirmenbuchFnr` (unique/partiell, Idempotenz der Uebernahme), `Notes`,
  `NextFollowUpAt` (treibt Faellig-Liste), `LastContactedAt`/`LastReplyAt`.
- `MarketingActivity` - Historie eines Kontakts (Anruf/Notiz/Statuswechsel/
  Wiedervorlage/Termin), FK auf Kontakt (Cascade). Wird zusammen mit gesendeten
  und eingegangenen Mails zur Timeline zusammengefuehrt; Mailversand erzeugt bewusst
  KEINE Aktivitaet (die Mail ist bereits ein eigener Eintrag).
- `MarketingEmailTemplate` - E-Mail-Vorlagen (Name unique, Betreff/Text mit
  Platzhaltern, aktiv/inaktiv, Reihenfolge). Ueber `/intern/marketing/vorlagen`
  ohne Deploy pflegbar; `MarketingTemplateSeeder` (Referenzdaten) legt Start-Vorlagen an.
- `MarketingEmail` - Versand-Historie mit SMTP-`MessageId` (Reply-Threading),
  Generierungs-Stichwoertern (Auswertung) und Status `Sent`/`LoggedOnly`.
- `MarketingInboundEmail` - Rueckmeldungen aus dem Postfach; `MessageId` unique
  (Sync-Idempotenz), FKs auf Kontakt (Cascade) und beantwortete Mail (SetNull).

Migrations liegen in BEIDEN Provider-Sets (`Core.Data/Migrations` SQLite,
`Core.Data.Migrations.Postgres`), Demo-Kontakte im `MarketingSeeder` (IsDemoData).

## Vorlagen-Platzhalter

`MarketingTemplateRenderer` ersetzt aus dem Kontakt: `{anrede}` (mit Ansprechpartner
"Guten Tag {Name}", sonst "Sehr geehrte Damen und Herren" - das Geschlecht ist im
Firmenbuch nicht bekannt), `{firma}`, `{name}`, `{ort}`. Ersetzung passiert
serverseitig (Backend-First); der eingesetzte Text bleibt im Editor aenderbar.
Die Signatur ist NICHT Teil der Vorlage (kommt beim Versand aus dem Impressum).

## Ablauf

1. **Generate** (optional): `IMarketingEmailGenerator` erstellt aus Stichwoertern
   (+ optionalem Empfaenger-Namen fuer die Anrede) einen Entwurf - die Compose-Seite
   erlaubt auch komplett selbst geschriebene E-Mails (`/email/signature` liefert
   dafuer die Signatur-Vorschau ohne Generierung). Provider:
   - `Mock` (Default): Platzhalter-Text ohne KI fuer lokale Entwicklung.
   - `AiConnector`: Prompt im Workspace `projects/heimatplatz`, Section
     `sections/marketing/email` (AGENTS.md definiert Rolle, Ton und das
     JSON-Ausgabeformat `{"subject", "body"}` - geparst von
     `MarketingEmailOutputParser`).
2. **Send**: `MarketingEmailComposer` baut HTML + Plaintext und haengt die Signatur
   an; Versand ueber `IEmailSender` (Core.Email). Optional geht eine offene Kopie
   an eine CC-Adresse (`CcEmail`) und/oder eine verdeckte Kopie an eine
   BCC-Adresse (`BccEmail`) - beide werden bewusst NICHT als Kontakt angelegt
   und nicht in der Historie gespeichert (die Gesendet-Kopie im Postfach enthaelt
   die Cc-/Bcc-Header). Danach Kontakt-Upsert (Lead->Contacted, LastContactedAt) +
   Historien-Zeile mit Message-Id.
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
- `Heimatplatz.Api.Features.Firmenbuch` (`FirmenbuchCompany` als Lead-Quelle des Firmenpools)
- `Heimatplatz.Api.Core.Data` (+Seeding) (AppDbContext, Entities, Seeder)
- `Heimatplatz.Api.Core.Email` (`IEmailSender`, `EmailOptions`, MailKit/IMAP)
- `Heimatplatz.Api.Core.AiConnectorClient` (generierter `RunPromptHttpRequest`-Client)
