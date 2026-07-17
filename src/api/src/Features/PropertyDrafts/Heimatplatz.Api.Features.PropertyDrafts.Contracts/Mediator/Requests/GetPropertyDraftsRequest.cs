using Heimatplatz.Api.Features.Properties.Contracts;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.PropertyDrafts.Contracts.Mediator.Requests;

/// <summary>
/// Listet alle Inserat-Entwuerfe des angemeldeten Verkaeufers
/// (nur Summary-Daten, kein Payload).
/// </summary>
public record GetPropertyDraftsRequest : IRequest<GetPropertyDraftsResponse>;

/// <summary>
/// Listeneintrag fuer die "Entwuerfe"-Sektion in "Meine Immobilien".
/// </summary>
public record PropertyDraftListItemDto(
    Guid Id,
    string? Title,
    PropertyType? Type,
    int StepIndex,
    string? FirstImageUrl,
    DateTimeOffset UpdatedAt
);

/// <summary>
/// Alle Entwuerfe des Nutzers, neueste zuerst.
/// </summary>
public record GetPropertyDraftsResponse(
    List<PropertyDraftListItemDto> Drafts
);
