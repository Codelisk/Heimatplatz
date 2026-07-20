using Heimatplatz.Api.Core.Email;

namespace Heimatplatz.Api.Features.Marketing.Services;

/// <summary>
/// Baut aus Betreff + Fliesstext die fertige E-Mail (HTML + Plaintext) inklusive
/// professioneller Signatur mit den Kontaktdaten aus dem Impressum (LegalSettings).
/// </summary>
public interface IMarketingEmailComposer
{
    /// <summary>Plaintext-Signatur fuer die Vorschau im Intern-Bereich.</summary>
    Task<string> GetSignatureTextAsync(CancellationToken ct = default);

    /// <summary>Fertige Mail: Text-Absaetze als HTML formatiert, Signatur angehaengt.</summary>
    Task<EmailMessage> ComposeAsync(string toAddress, string subject, string body, CancellationToken ct = default);
}
