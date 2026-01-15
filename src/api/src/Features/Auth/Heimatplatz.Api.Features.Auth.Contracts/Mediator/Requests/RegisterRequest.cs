using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Auth.Contracts.Mediator.Requests;

/// <summary>
/// Request zur Benutzerregistrierung
/// </summary>
public record RegisterRequest(
    string Vorname,
    string Nachname,
    string Email,
    string Passwort
) : IRequest<RegisterResponse>;

/// <summary>
/// Response nach erfolgreicher Registrierung
/// </summary>
public record RegisterResponse(
    Guid UserId,
    string Email,
    string FullName
);
