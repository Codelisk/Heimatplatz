# Heimatplatz.Api.Core.Email

Zentraler E-Mail-Versand fuer die API (Verifikations-Mails, Passwort-Reset etc.).

## Zweck und Verantwortlichkeiten

- `IEmailSender` - Abstraktion fuer den Versand einer `EmailMessage` (HTML + Plain-Text-Alternative)
- `SmtpEmailSender` - produktiver Versand via MailKit/SMTP (Hetzner-Webhosting-Mailbox `info@heimatplatz.at`, Server `mail.your-server.de`)
- `LoggingEmailSender` - Fallback ohne SMTP-Konfiguration: loggt die Mail nur (lokale Entwicklung; Links koennen aus dem Log kopiert werden)
- `EmailOptions` - Konfiguration (Section `Email`)

Die fachlichen Mail-Inhalte (Templates, Links) liegen NICHT hier, sondern bei den Features
(z.B. `Heimatplatz.Api.Features.Auth/Services/AuthEmailService.cs`).

## Konfiguration

```json
{
  "Email": {
    "SmtpHost": "mail.your-server.de",
    "SmtpPort": 587,
    "SmtpUsername": "info@heimatplatz.at",
    "SmtpPassword": "<secret>",
    "FromAddress": "info@heimatplatz.at",
    "FromName": "Heimatplatz",
    "TimeoutSeconds": 15,
    "FrontendBaseUrl": "https://heimatplatz.at"
  }
}
```

- Ohne `SmtpHost` wird der `LoggingEmailSender` registriert - es geht keine echte Mail raus.
- `SmtpPort` 587 = STARTTLS, 465 = implizites TLS. Unverschluesselt wird nie verbunden.
- `FrontendBaseUrl` ist die Basis fuer Links in Mails (`/email-bestaetigen/`, `/passwort-zuruecksetzen/`);
  auf der Test-Umgebung `https://test.heimatplatz.at`.
- Produktiv kommen die Werte aus `deploy/hetzner/docker-compose.yml` + Server-`.env`
  (`EMAIL_SMTP_PASSWORD`).

## Verwendung

```csharp
// Registrierung (Core.Startup macht das bereits):
services.AddEmailFeature(configuration);

// In einem Handler/Service:
await emailSender.SendAsync(new EmailMessage(
    ToAddress: user.Email,
    Subject: "Willkommen",
    HtmlBody: "<p>...</p>",
    TextBody: "..."
), cancellationToken);
```

`SendAsync` wirft bei Versand-Fehlern - der Aufrufer entscheidet, ob das den Request
fehlschlagen laesst (z.B. Registrierung: nein, nur Warnung loggen).

## Abhaengigkeiten

- MailKit (SMTP-Client)
- Microsoft.Extensions.* (Options, Logging, DI)
- Keine Abhaengigkeit auf andere Heimatplatz-Projekte
