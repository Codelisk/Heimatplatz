using Shiny.Mediator;

namespace Heimatplatz.Api.Features.PropertyDrafts.Contracts.Mediator.Requests;

/// <summary>
/// Veroeffentlicht einen Entwurf: mappt den Payload serverseitig auf CreateProperty
/// (volle Validierung, SellerInfoResolver, Journal und Events feuern normal) und
/// loescht bei Erfolg den Entwurf OHNE die Medien-Dateien (die Immobilie referenziert sie).
/// Id im Body - Shiny-Mediator-OpenAPI-Generator-Kompatibilitaet.
/// </summary>
public class PublishPropertyDraftRequest : IRequest<PublishPropertyDraftResponse>
{
    public Guid Id { get; set; }
}

/// <summary>
/// Die neu erstellte Immobilie.
/// </summary>
public record PublishPropertyDraftResponse(
    Guid PropertyId,
    string Title,
    DateTimeOffset CreatedAt
);
