using Heimatplatz.Api.Features.Properties.Contracts;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;

/// <summary>
/// Request zum Erstellen einer neuen Immobilie
/// </summary>
public record CreatePropertyRequest(
    string Titel,
    string Adresse,
    string Ort,
    string Plz,
    decimal Preis,
    PropertyType Typ,
    SellerType AnbieterTyp,
    string AnbieterName,
    string? Beschreibung = null,
    int? WohnflaecheM2 = null,
    int? GrundstuecksflaecheM2 = null,
    int? Zimmer = null,
    int? Baujahr = null,
    List<string>? Ausstattung = null,
    List<string>? BildUrls = null
) : IRequest<CreatePropertyResponse>;

/// <summary>
/// Response nach erfolgreicher Erstellung einer Immobilie
/// </summary>
public record CreatePropertyResponse(
    Guid PropertyId,
    string Titel,
    DateTimeOffset CreatedAt
);
