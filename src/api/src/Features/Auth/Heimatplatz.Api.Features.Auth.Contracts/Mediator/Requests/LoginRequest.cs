using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Auth.Contracts.Mediator.Requests;

/// <summary>
/// Request fuer Benutzer-Login
/// </summary>
public record LoginRequest(
    string Email,
    string Passwort
) : IRequest<LoginResponse>;

/// <summary>
/// Response nach erfolgreichem Login mit Tokens
/// </summary>
public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    Guid UserId,
    string Email,
    string FullName,
    DateTimeOffset ExpiresAt
);
