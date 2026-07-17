using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Auth.Contracts.Mediator.Requests;

/// <summary>
/// Request zum Setzen eines neuen Passworts ueber den Token aus der "Passwort vergessen"-Mail.
/// Widerruft aus Sicherheitsgruenden alle bestehenden Refresh Tokens (alle Geraete abgemeldet).
/// </summary>
public record ResetPasswordRequest(
    string Token,
    string NewPassword
) : IRequest<ResetPasswordResponse>;

/// <summary>
/// Response nach erfolgreichem Passwort-Reset (kein Auto-Login - der Benutzer meldet
/// sich anschliessend mit dem neuen Passwort an)
/// </summary>
public record ResetPasswordResponse(
    string Message
);
