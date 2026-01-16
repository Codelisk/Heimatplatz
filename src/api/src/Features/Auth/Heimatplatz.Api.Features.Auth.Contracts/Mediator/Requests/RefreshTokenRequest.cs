using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Auth.Contracts.Mediator.Requests;

/// <summary>
/// Request zum Erneuern eines Access Tokens mittels Refresh Token
/// </summary>
public record RefreshTokenRequest(
    string RefreshToken
) : IRequest<RefreshTokenResponse>;

/// <summary>
/// Response mit neuen Tokens nach erfolgreichem Refresh
/// </summary>
public record RefreshTokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt
);
