namespace Heimatplatz.Api.Core.Email;

/// <summary>
/// Konfiguration fuer den E-Mail-Versand (Section "Email").
/// Produktiv laeuft der Versand ueber die Hetzner-Webhosting-Mailbox info@heimatplatz.at
/// (SMTP mail.your-server.de). Ohne konfigurierten SmtpHost wird nicht versendet,
/// sondern nur geloggt (LoggingEmailSender) - praktisch fuer lokale Entwicklung.
/// </summary>
public class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>SMTP-Server, z.B. mail.your-server.de. Leer = Versand deaktiviert (Logging-Fallback).</summary>
    public string SmtpHost { get; set; } = "";

    /// <summary>587 = STARTTLS (Default), 465 = implizites TLS</summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>SMTP-Login, bei Hetzner die vollstaendige Mail-Adresse</summary>
    public string SmtpUsername { get; set; } = "";

    public string SmtpPassword { get; set; } = "";

    /// <summary>Absender-Adresse der ausgehenden Mails</summary>
    public string FromAddress { get; set; } = "info@heimatplatz.at";

    /// <summary>Anzeigename des Absenders</summary>
    public string FromName { get; set; } = "Heimatplatz";

    /// <summary>
    /// Timeout fuer Verbindung + Versand. Bewusst kurz: Handler warten synchron auf den
    /// Versand (z.B. bei der Registrierung), ein haengender SMTP-Server darf keine
    /// Requests blockieren.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 15;

    /// <summary>
    /// Basis-URL des Web-Frontends fuer Links in E-Mails (Verifikation, Passwort-Reset).
    /// Test-Umgebung: https://test.heimatplatz.at
    /// </summary>
    public string FrontendBaseUrl { get; set; } = "https://heimatplatz.at";

    /// <summary>
    /// IMAP-Server zum Abrufen des Postfachs (Marketing-Posteingang). Leer = gleicher
    /// Host wie SmtpHost (bei Hetzner-Webhosting ist mail.your-server.de beides),
    /// Zugangsdaten sind dieselben wie fuer SMTP.
    /// </summary>
    public string ImapHost { get; set; } = "";

    /// <summary>993 = implizites TLS (Default)</summary>
    public int ImapPort { get; set; } = 993;

    /// <summary>Effektiver IMAP-Host: expliziter ImapHost oder Fallback auf SmtpHost</summary>
    public string EffectiveImapHost => string.IsNullOrWhiteSpace(ImapHost) ? SmtpHost : ImapHost;

    /// <summary>Erst mit gesetztem SmtpHost wird tatsaechlich versendet</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(SmtpHost);

    /// <summary>Postfach-Abruf moeglich (Host + Zugangsdaten vorhanden)</summary>
    public bool IsImapConfigured =>
        !string.IsNullOrWhiteSpace(EffectiveImapHost) && !string.IsNullOrWhiteSpace(SmtpUsername);
}
