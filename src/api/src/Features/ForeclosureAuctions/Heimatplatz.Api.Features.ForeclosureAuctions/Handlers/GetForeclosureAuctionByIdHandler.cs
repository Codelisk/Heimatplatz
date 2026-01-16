using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.ForeclosureAuctions.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.ForeclosureAuctions.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.ForeclosureAuctions.Handlers;

/// <summary>
/// Handler fuer GetForeclosureAuctionByIdRequest - gibt eine einzelne Zwangsversteigerung zurueck
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class GetForeclosureAuctionByIdHandler(AppDbContext dbContext)
    : IRequestHandler<GetForeclosureAuctionByIdRequest, GetForeclosureAuctionByIdResponse>
{
    [MediatorHttpGet("/api/foreclosure-auctions/{Id}", OperationId = "GetForeclosureAuctionById")]
    public async Task<GetForeclosureAuctionByIdResponse> Handle(
        GetForeclosureAuctionByIdRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.Set<ForeclosureAuction>()
            .FirstOrDefaultAsync(fa => fa.Id == request.Id, cancellationToken);

        if (entity == null)
            return new GetForeclosureAuctionByIdResponse { Auction = null };

        var dto = new ForeclosureAuctionDto
        {
            Id = entity.Id,
            AuctionDate = entity.AuctionDate,
            Address = entity.Address,
            City = entity.City,
            PostalCode = entity.PostalCode,
            State = entity.State,
            Category = entity.Category,
            ObjectDescription = entity.ObjectDescription,
            EdictUrl = entity.EdictUrl,
            Notes = entity.Notes,
            EstimatedValue = entity.EstimatedValue,
            MinimumBid = entity.MinimumBid,
            CaseNumber = entity.CaseNumber,
            Court = entity.Court,
            CreatedAt = entity.CreatedAt
        };

        return new GetForeclosureAuctionByIdResponse { Auction = dto };
    }
}
