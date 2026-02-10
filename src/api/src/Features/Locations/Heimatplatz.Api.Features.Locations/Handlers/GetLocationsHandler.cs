using Heimatplatz.Api;
using Heimatplatz.Api.Authorization;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Locations.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Locations.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Locations.Handlers;

/// <summary>
/// Handler fuer GetLocationsRequest - gibt die Location-Hierarchie zurueck
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/locations")]
public class GetLocationsHandler(
    AppDbContext dbContext
) : IRequestHandler<GetLocationsRequest, GetLocationsResponse>
{
    [MediatorHttpGet("/", OperationId = "GetLocations")]
    public async Task<GetLocationsResponse> Handle(GetLocationsRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        var query = dbContext.Set<FederalProvince>()
            .Include(fp => fp.Districts)
                .ThenInclude(d => d.Municipalities)
            .AsQueryable();

        // Optional nach Bundesland filtern
        if (!string.IsNullOrWhiteSpace(request.FederalProvinceKey))
            query = query.Where(fp => fp.Key == request.FederalProvinceKey);

        var provinces = await query
            .OrderBy(fp => fp.Key)
            .ToListAsync(cancellationToken);

        var dtos = provinces.Select(fp => new FederalProvinceDto(
            fp.Id,
            fp.Key,
            fp.Name,
            fp.Districts
                .OrderBy(d => d.Name)
                .Select(d => new DistrictDto(
                    d.Id,
                    d.Key,
                    d.Code,
                    d.Name,
                    d.FederalProvinceId,
                    d.Municipalities
                        .OrderBy(m => m.Name)
                        .Select(m => new MunicipalityDto(
                            m.Id,
                            m.Key,
                            m.Code,
                            m.Name,
                            m.PostalCode,
                            m.Status,
                            m.DistrictId
                        ))
                        .ToList()
                ))
                .ToList()
        )).ToList();

        return new GetLocationsResponse(dtos);
    }
}
