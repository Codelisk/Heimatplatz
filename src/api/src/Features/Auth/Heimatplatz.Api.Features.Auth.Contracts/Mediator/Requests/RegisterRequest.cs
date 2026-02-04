using Heimatplatz.Api.Features.Auth.Contracts.Enums;
using Heimatplatz.Api.Features.Properties.Contracts;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Auth.Contracts.Mediator.Requests;

/// <summary>
/// Request zur Benutzerregistrierung
/// </summary>
public record RegisterRequest(
    string Vorname,
    string Nachname,
    string Email,
    string Passwort,
    List<UserRoleType>? Roles = null,
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
