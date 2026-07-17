using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Auth.Contracts.Mediator.Requests;

/// <summary>
/// Request zum erneuten Versand der Verifikations-Mail an den eingeloggten Benutzer
/// (Benutzer kommt aus dem JWT).
/// </summary>
public record ResendVerificationEmailRequest : IRequest<ResendVerificationEmailResponse>;

/// <summary>
/// Response nach Versand der Verifikations-Mail.
/// AlreadyVerified=true bedeutet: es wurde nichts versendet, die Adresse ist schon bestaetigt.
/// </summary>
public record ResendVerificationEmailResponse(
    bool AlreadyVerified = false
);
