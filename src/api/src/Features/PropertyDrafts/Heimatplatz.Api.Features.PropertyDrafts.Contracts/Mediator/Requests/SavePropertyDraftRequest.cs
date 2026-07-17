using Heimatplatz.Api.Features.PropertyDrafts.Contracts.Models;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.PropertyDrafts.Contracts.Mediator.Requests;

/// <summary>
/// Upsert eines Inserat-Entwurfs: ohne Id wird ein neuer Entwurf angelegt,
/// mit Id der bestehende aktualisiert (Ownership-geprueft).
/// Id im Body statt in der Route - Shiny-Mediator-OpenAPI-Generator-Kompatibilitaet
/// (gleiche Praezedenz wie UpdatePropertyRequest).
/// </summary>
public class SavePropertyDraftRequest : IRequest<SavePropertyDraftResponse>
{
    public Guid? Id { get; set; }
    public PropertyDraftData Data { get; set; } = new();
}

/// <summary>
/// Response mit der (ggf. neu vergebenen) Entwurfs-Id.
/// </summary>
public record SavePropertyDraftResponse(
    Guid Id,
    DateTimeOffset UpdatedAt
);
