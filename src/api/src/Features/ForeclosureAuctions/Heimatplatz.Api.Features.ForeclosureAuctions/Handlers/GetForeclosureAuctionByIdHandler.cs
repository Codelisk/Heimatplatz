using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.ForeclosureAuctions.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.ForeclosureAuctions.Data.Entities;
using Heimatplatz.Api.Features.Properties.Handlers;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.ForeclosureAuctions.Handlers;

[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/foreclosure-auctions")]
public class GetForeclosureAuctionByIdHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration
) : IRequestHandler<GetForeclosureAuctionByIdRequest, GetForeclosureAuctionByIdResponse>
{
    [MediatorHttpGet("/{Id}", OperationId = "GetForeclosureAuctionById")]
    public async Task<GetForeclosureAuctionByIdResponse> Handle(
        GetForeclosureAuctionByIdRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.Set<ForeclosureAuction>()
            .FirstOrDefaultAsync(fa => fa.Id == request.Id, cancellationToken);

        if (entity == null)
            return new GetForeclosureAuctionByIdResponse { Auction = null };

        var baseUrl = GetPropertiesHandler.ResolveApiBaseUrl(httpContextAccessor, configuration);

        return new GetForeclosureAuctionByIdResponse
        {
            Auction = GetForeclosureAuctionsHandler.MapToDto(entity, baseUrl)
        };
    }
}
