using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Partners.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Partners.Data.Entities;
using Heimatplatz.Api.Features.Partners.Services;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Partners.Handlers;

/// <summary>
/// Oeffentliche Partnerliste fuer /partner/ - nur sichtbare Partner in
/// Anzeige-Reihenfolge, mit Live-Inseratszahl ueber Property.SourceName.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api")]
public class GetPartnersHandler(AppDbContext dbContext)
    : IRequestHandler<GetPartnersRequest, GetPartnersResponse>
{
    [MediatorHttpGet("/partners", OperationId = "GetPartners")]
    public async Task<GetPartnersResponse> Handle(GetPartnersRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        var partners = await dbContext.Set<Partner>()
            .AsNoTracking()
            .Where(p => p.IsVisible)
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);

        var counts = await PartnerListingCounts.LoadAsync(dbContext, partners, cancellationToken);

        return new GetPartnersResponse(partners.Select(p => PartnerMapper.ToDto(p, counts)).ToList());
    }
}
