using Heimatplatz.Api.Features.PropertyDrafts.Contracts.Models;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.PropertyDrafts.Contracts.Mediator.Requests;

/// <summary>
/// Laedt einen einzelnen Inserat-Entwurf inklusive Payload (fuer das Fortsetzen im Wizard).
/// </summary>
public record GetPropertyDraftRequest(
    Guid Id
) : IRequest<GetPropertyDraftResponse>;

/// <summary>
/// Vollstaendiger Entwurf zum Wiederherstellen des Wizard-Zustands.
/// </summary>
public record GetPropertyDraftResponse(
    Guid Id,
    PropertyDraftData Data,
    DateTimeOffset UpdatedAt
);
