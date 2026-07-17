using Shiny.Mediator;

namespace Heimatplatz.Api.Features.PropertyDrafts.Contracts.Mediator.Requests;

/// <summary>
/// Loescht einen Inserat-Entwurf inklusive der nur von ihm referenzierten
/// hochgeladenen Medien-Dateien.
/// </summary>
public record DeletePropertyDraftRequest(
    Guid Id
) : IRequest<DeletePropertyDraftResponse>;

/// <summary>
/// Ergebnis der Entwurfs-Loeschung.
/// </summary>
public record DeletePropertyDraftResponse(
    bool Success
);
