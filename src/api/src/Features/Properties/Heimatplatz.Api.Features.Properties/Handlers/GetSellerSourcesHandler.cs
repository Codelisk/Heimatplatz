using Heimatplatz.Api;
using Heimatplatz.Api.Authorization;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Handlers;

/// <summary>
/// Handler for GetSellerSourcesRequest - returns available seller sources (brokers/portals)
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/seller-sources")]
public class GetSellerSourcesHandler(
    AppDbContext dbContext
) : IRequestHandler<GetSellerSourcesRequest, GetSellerSourcesResponse>
{
    [MediatorHttpGet("/", OperationId = "GetSellerSources", AuthorizationPolicies = [AuthorizationPolicies.RequireAnyRole])]
    public async Task<GetSellerSourcesResponse> Handle(GetSellerSourcesRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        var sources = await dbContext.Set<SellerSource>()
            .OrderBy(ss => ss.Name)
            .Select(ss => new SellerSourceDto(ss.Id, ss.Name, ss.SellerType))
            .ToListAsync(cancellationToken);

        return new GetSellerSourcesResponse(sources);
    }
}
