using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Auth.Contracts.Mediator.Requests;

/// <summary>
/// Request zum Aendern des eigenen Passworts (erfordert das aktuelle Passwort).
/// Widerruft aus Sicherheitsgruenden alle bestehenden Refresh Tokens.
/// </summary>
public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
) : IRequest<ChangePasswordResponse>;

/// <summary>
/// Response nach Passwort-Aenderung mit neuem Token-Paar
/// (alle alten Refresh Tokens sind widerrufen)
/// </summary>
public record ChangePasswordResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt
);
