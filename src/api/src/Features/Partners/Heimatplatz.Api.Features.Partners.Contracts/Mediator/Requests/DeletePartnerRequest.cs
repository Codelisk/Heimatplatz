using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Partners.Contracts.Mediator.Requests;

/// <summary>
/// Loescht einen Partner endgueltig (inkl. hochgeladenem Logo). Fuer "nur ausblenden"
/// gibt es IsVisible im SavePartnerRequest. Admin-only.
/// </summary>
public record DeletePartnerRequest(Guid Id) : IRequest<DeletePartnerResponse>;

public record DeletePartnerResponse(
    bool Success,
    string? Error
);
