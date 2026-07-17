using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Auth.Contracts.Mediator.Requests;

/// <summary>
/// Request zum Bestaetigen der E-Mail-Adresse ueber den Token aus der Verifikations-Mail
/// (anonym aufrufbar - der Link wird auch in nicht eingeloggten Browsern geoeffnet).
/// </summary>
public record VerifyEmailRequest(
    string Token
) : IRequest<VerifyEmailResponse>;

/// <summary>
/// Response nach erfolgreicher E-Mail-Bestaetigung
/// </summary>
public record VerifyEmailResponse(
    string Email,
    bool AlreadyVerified = false
);
