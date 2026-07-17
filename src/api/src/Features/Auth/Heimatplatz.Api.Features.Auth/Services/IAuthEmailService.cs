using Heimatplatz.Api.Features.Auth.Data.Entities;

namespace Heimatplatz.Api.Features.Auth.Services;

/// <summary>
/// Versand der Auth-bezogenen Mails (Verifikation, Passwort-Reset) inkl. Token-Erzeugung.
/// Wirft bei Versand-Fehlern - der Aufrufer entscheidet, ob das den Request fehlschlagen
/// laesst (Registrierung: nein; expliziter Neu-Versand: ja).
/// </summary>
public interface IAuthEmailService
{
    /// <summary>Erzeugt einen Verifikations-Token und schickt die Bestaetigungs-Mail</summary>
    Task SendVerificationEmailAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>Erzeugt einen Reset-Token und schickt die "Passwort vergessen"-Mail</summary>
    Task SendPasswordResetEmailAsync(User user, CancellationToken cancellationToken = default);
}
