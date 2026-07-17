using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Auth.Contracts.Mediator.Requests;

/// <summary>
/// Request zur Benutzerregistrierung.
/// Jeder Benutzer ist implizit Kaeufer; wer verkaufen will, gibt einen SellerType an
/// (Broker und PropertyManager zusaetzlich einen Firmennamen).
/// </summary>
public record RegisterRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    SellerType? SellerType = null,
    string? CompanyName = null
) : IRequest<RegisterResponse>;

/// <summary>
/// Response nach erfolgreicher Registrierung (mit automatischem Login)
/// </summary>
public record RegisterResponse(
    string AccessToken,
    string RefreshToken,
    Guid UserId,
    string Email,
    string FullName,
    DateTimeOffset ExpiresAt
);
