using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Admin.Services;
using Heimatplatz.Api.Features.Partners.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Partners.Data.Entities;
using Heimatplatz.Api.Features.Partners.Services;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Partners.Handlers;

/// <summary>
/// Vollstaendige Partnerliste fuer /intern/partner/ - auch ausgeblendete Eintraege,
/// damit die Pflege-UI sie bearbeiten und reaktivieren kann.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin/partners")]
public class GetAdminPartnersHandler(
    AppDbContext dbContext,
    IAdminAccessGuard accessGuard
) : IRequestHandler<GetAdminPartnersRequest, GetAdminPartnersResponse>
{
    [MediatorHttpGet("", OperationId = "GetAdminPartners")]
    public async Task<GetAdminPartnersResponse> Handle(GetAdminPartnersRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        accessGuard.EnsureAuthorized();

        var partners = await dbContext.Set<Partner>()
            .AsNoTracking()
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);

        var counts = await PartnerListingCounts.LoadAsync(dbContext, partners, cancellationToken);

        return new GetAdminPartnersResponse(partners.Select(p => PartnerMapper.ToDto(p, counts)).ToList());
    }
}
