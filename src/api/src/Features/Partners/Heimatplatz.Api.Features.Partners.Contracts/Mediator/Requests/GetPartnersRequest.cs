using Heimatplatz.Api.Features.Partners.Contracts.Models;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Partners.Contracts.Mediator.Requests;

/// <summary>
/// Oeffentliche Partnerliste fuer /partner/ - nur sichtbare Partner,
/// sortiert nach DisplayOrder, mit Live-Inseratszahl.
/// </summary>
public record GetPartnersRequest : IRequest<GetPartnersResponse>;

public record GetPartnersResponse(List<PartnerDto> Partners);
