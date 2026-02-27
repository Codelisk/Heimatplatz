using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.ForeclosureAuctions.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.ForeclosureAuctions.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.ForeclosureAuctions.Handlers;

[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/foreclosure-auctions")]
public class GetForeclosureAuctionByIdHandler(AppDbContext dbContext)
    : IRequestHandler<GetForeclosureAuctionByIdRequest, GetForeclosureAuctionByIdResponse>
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

        return new GetForeclosureAuctionByIdResponse
        {
            Auction = GetForeclosureAuctionsHandler.MapToDto(entity)
        };
    }
}
