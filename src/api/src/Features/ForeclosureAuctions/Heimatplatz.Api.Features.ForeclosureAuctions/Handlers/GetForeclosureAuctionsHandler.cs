using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.ForeclosureAuctions.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.ForeclosureAuctions.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.ForeclosureAuctions.Handlers;

/// <summary>
/// Handler fuer GetForeclosureAuctionsRequest - gibt gefilterte Zwangsversteigerungen zurueck
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class GetForeclosureAuctionsHandler(AppDbContext dbContext)
    : IRequestHandler<GetForeclosureAuctionsRequest, GetForeclosureAuctionsResponse>
{
    [MediatorHttpGet("/api/foreclosure-auctions", OperationId = "GetForeclosureAuctions")]
    public async Task<GetForeclosureAuctionsResponse> Handle(
        GetForeclosureAuctionsRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Set<ForeclosureAuction>().AsQueryable();

        // Filter anwenden
        if (request.Category.HasValue)
            query = query.Where(fa => fa.Category == request.Category.Value);

        if (!string.IsNullOrWhiteSpace(request.City))
            query = query.Where(fa => fa.City.Contains(request.City));

        if (!string.IsNullOrWhiteSpace(request.PostalCode))
            query = query.Where(fa => fa.PostalCode.StartsWith(request.PostalCode));

        if (request.AuctionDateFrom.HasValue)
            query = query.Where(fa => fa.AuctionDate >= request.AuctionDateFrom.Value);

        if (request.AuctionDateTo.HasValue)
            query = query.Where(fa => fa.AuctionDate <= request.AuctionDateTo.Value);

        if (request.MaxEstimatedValue.HasValue)
            query = query.Where(fa => fa.EstimatedValue <= request.MaxEstimatedValue.Value);

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(fa => fa.Status == request.Status);

        // Laden und Sortieren (SQLite unterstuetzt DateTimeOffset nicht in ORDER BY)
        var entities = await query.ToListAsync(cancellationToken);
        var auctions = entities
            .OrderBy(fa => fa.AuctionDate)
            .Select(fa => new ForeclosureAuctionDto
            {
                Id = fa.Id,
                AuctionDate = fa.AuctionDate,
                Category = fa.Category,
                ObjectDescription = fa.ObjectDescription,
                Status = fa.Status,
                Address = fa.Address,
                City = fa.City,
                PostalCode = fa.PostalCode,
                RegistrationNumber = fa.RegistrationNumber,
                CadastralMunicipality = fa.CadastralMunicipality,
                PlotNumber = fa.PlotNumber,
                SheetNumber = fa.SheetNumber,
                TotalArea = fa.TotalArea,
                BuildingArea = fa.BuildingArea,
                GardenArea = fa.GardenArea,
                PlotArea = fa.PlotArea,
                YearBuilt = fa.YearBuilt,
                NumberOfRooms = fa.NumberOfRooms,
                ZoningDesignation = fa.ZoningDesignation,
                BuildingCondition = fa.BuildingCondition,
                EstimatedValue = fa.EstimatedValue,
                MinimumBid = fa.MinimumBid,
                ViewingDate = fa.ViewingDate,
                BiddingDeadline = fa.BiddingDeadline,
                OwnershipShare = fa.OwnershipShare,
                CaseNumber = fa.CaseNumber,
                Court = fa.Court,
                EdictUrl = fa.EdictUrl,
                Notes = fa.Notes,
                FloorPlanUrl = fa.FloorPlanUrl,
                SitePlanUrl = fa.SitePlanUrl,
                LongAppraisalUrl = fa.LongAppraisalUrl,
                ShortAppraisalUrl = fa.ShortAppraisalUrl,
                CreatedAt = fa.CreatedAt
            })
            .ToList();

        return new GetForeclosureAuctionsResponse { Auctions = auctions };
    }
}
