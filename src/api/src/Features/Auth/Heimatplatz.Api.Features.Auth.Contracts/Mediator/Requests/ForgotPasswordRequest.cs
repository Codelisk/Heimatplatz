using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Auth.Contracts.Mediator.Requests;

/// <summary>
/// Request fuer "Passwort vergessen": verschickt eine Reset-Mail, WENN ein Konto mit der
/// Adresse existiert. Die Response ist immer identisch (kein User-Enumeration-Leak).
/// </summary>
public record ForgotPasswordRequest(
    string Email
) : IRequest<ForgotPasswordResponse>;

/// <summary>
/// Generische Response - unabhaengig davon, ob ein Konto existiert
/// </summary>
public record ForgotPasswordResponse(
    string Message
);
