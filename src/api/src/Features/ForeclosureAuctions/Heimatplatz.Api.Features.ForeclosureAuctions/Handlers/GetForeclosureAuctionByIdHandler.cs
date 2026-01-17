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
            Category = entity.Category,
            ObjectDescription = entity.ObjectDescription,
            Status = entity.Status,
            Address = entity.Address,
            City = entity.City,
            PostalCode = entity.PostalCode,
            RegistrationNumber = entity.RegistrationNumber,
            CadastralMunicipality = entity.CadastralMunicipality,
            PlotNumber = entity.PlotNumber,
            SheetNumber = entity.SheetNumber,
            TotalArea = entity.TotalArea,
            BuildingArea = entity.BuildingArea,
            GardenArea = entity.GardenArea,
            PlotArea = entity.PlotArea,
            YearBuilt = entity.YearBuilt,
            NumberOfRooms = entity.NumberOfRooms,
            ZoningDesignation = entity.ZoningDesignation,
            BuildingCondition = entity.BuildingCondition,
            EstimatedValue = entity.EstimatedValue,
            MinimumBid = entity.MinimumBid,
            ViewingDate = entity.ViewingDate,
            BiddingDeadline = entity.BiddingDeadline,
            OwnershipShare = entity.OwnershipShare,
            CaseNumber = entity.CaseNumber,
            Court = entity.Court,
            EdictUrl = entity.EdictUrl,
            Notes = entity.Notes,
            FloorPlanUrl = entity.FloorPlanUrl,
            SitePlanUrl = entity.SitePlanUrl,
            LongAppraisalUrl = entity.LongAppraisalUrl,
            ShortAppraisalUrl = entity.ShortAppraisalUrl,
            CreatedAt = entity.CreatedAt
        };

        return new GetForeclosureAuctionByIdResponse { Auction = dto };
    }
}
