using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;

/// <summary>
/// Request zum Abrufen einer gefilterten Immobilien-Liste
/// </summary>
public record GetPropertiesRequest(
    PropertyType? Typ = null,
    SellerType? AnbieterTyp = null,
    decimal? PreisMin = null,
    decimal? PreisMax = null,
    int? FlaecheMin = null,
    int? FlaecheMax = null,
    int? ZimmerMin = null,
    string? Ort = null,
    int Skip = 0,
    int Take = 20
) : IRequest<GetPropertiesResponse>;

/// <summary>
/// Response mit Immobilien-Liste und Gesamtanzahl
/// </summary>
public record GetPropertiesResponse(
    List<PropertyListItemDto> Immobilien,
    int Gesamt
);
