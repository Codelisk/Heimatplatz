using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Immobilien.Contracts.Mediator.Requests;

/// <summary>
/// Request fuer einzelne Immobilie mit allen Details
/// </summary>
public record GetImmobilieByIdRequest(Guid Id) : IRequest<GetImmobilieByIdResponse>;

/// <summary>
/// Response mit Immobilien-Details
/// </summary>
public record GetImmobilieByIdResponse(ImmobilieDetailDto? Immobilie);

/// <summary>
/// DTO fuer Immobilie mit allen Details
/// </summary>
public record ImmobilieDetailDto(
    Guid Id,
    string Titel,
    string? Beschreibung,
    ImmobilienTyp Typ,
    ImmobilienStatus Status,
    decimal Preis,
    string Waehrung,
    decimal Wohnflaeche,
    decimal? Grundstuecksflaeche,
    string Ort,
    string? Bezirk,
    string? Region,
    string Land,
    double? Breitengrad,
    double? Laengengrad,
    int? Zimmer,
    int? Schlafzimmer,
    int? Badezimmer,
    int? Baujahr,
    IReadOnlyList<ImmobilieBildDto> Bilder,
    DateTimeOffset ErstelltAm
);

/// <summary>
/// DTO fuer Immobilien-Bild
/// </summary>
public record ImmobilieBildDto(
    Guid Id,
    string Url,
    string? AltText,
    bool IstHauptbild
);
