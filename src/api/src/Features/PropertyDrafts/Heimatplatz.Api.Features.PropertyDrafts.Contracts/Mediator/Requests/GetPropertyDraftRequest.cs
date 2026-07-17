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
/// Vollstaendiger Entwurf zum Wiederherstellen des Wizard-Zustands, inklusive des
/// Zustands der KI-Beschreibungs-Generierung (eigene Spalten, nicht im Payload -
/// damit Auto-Saves des Payloads den Job-Fortschritt nie ueberschreiben).
/// </summary>
public record GetPropertyDraftResponse(
    Guid Id,
    PropertyDraftData Data,
    DateTimeOffset UpdatedAt,
    DraftDescriptionStatus DescriptionStatus,
    string? SubmittedDescriptionKeywords,
    string? GeneratedDescription,
    string? DescriptionError
);
