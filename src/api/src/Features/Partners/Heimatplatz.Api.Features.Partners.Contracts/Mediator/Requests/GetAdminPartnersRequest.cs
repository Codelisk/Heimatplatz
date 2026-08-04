using Heimatplatz.Api.Features.Partners.Contracts.Models;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Partners.Contracts.Mediator.Requests;

/// <summary>
/// Vollstaendige Partnerliste fuer /intern/partner/ - auch ausgeblendete Eintraege
/// (IsVisible=false), damit die Pflege-UI sie reaktivieren kann. Admin-only.
/// </summary>
public record GetAdminPartnersRequest : IRequest<GetAdminPartnersResponse>;

public record GetAdminPartnersResponse(List<PartnerDto> Partners);
