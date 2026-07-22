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
public class GetForeclosureAuctionsHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration
) : IRequestHandler<GetForeclosureAuctionsRequest, GetForeclosureAuctionsResponse>
{
    // Obergrenze pro Seite: schuetzt vor PageSize=1000000-Anfragen
    private const int MaxPageSize = 200;

    [MediatorHttpGet("/", OperationId = "GetForeclosureAuctions")]
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

        // DateTimeOffset-Vergleiche laufen auch auf SQLite in SQL - die Konverter im
        // AppDbContext speichern dort als long (UTC-Ticks), kein In-Memory-Umweg noetig
        if (request.AuctionDateFrom.HasValue)
        {
            var auctionDateFrom = request.AuctionDateFrom.Value.ToUniversalTime();
            query = query.Where(fa => fa.AuctionDate >= auctionDateFrom);
        }

        if (request.AuctionDateTo.HasValue)
        {
            var auctionDateTo = request.AuctionDateTo.Value.ToUniversalTime();
            query = query.Where(fa => fa.AuctionDate <= auctionDateTo);
        }

        if (request.MaxEstimatedValue.HasValue)
            query = query.Where(fa => fa.EstimatedValue <= request.MaxEstimatedValue.Value);

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(fa => fa.Status == request.Status);

        if (request.State.HasValue)
            query = query.Where(fa => fa.State == request.State.Value);

        if (request.IsActive.HasValue)
            query = query.Where(fa => fa.IsActive == request.IsActive.Value);

        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        // Total Count fuer Pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Build base URL for image proxy
        var baseUrl = GetPropertiesHandler.ResolveApiBaseUrl(httpContextAccessor, configuration);

        // Sortierung + Paging in der Datenbank (SQLite-DateTimeOffset-ORDER-BY laeuft
        // ueber die Konverter im AppDbContext) - nur die angeforderte Seite laden.
        var entities = await query
            .AsNoTracking()
            .OrderBy(fa => fa.AuctionDate)
            .ThenBy(fa => fa.Id) // stabiler Tiebreaker fuer deterministisches Paging
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var auctions = entities
            .Select(fa => MapToDto(fa, baseUrl, GetPropertiesHandler.ListThumbnailWidth))
            .ToList();

        return new GetForeclosureAuctionsResponse
        {
            Auctions = auctions,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    internal static ForeclosureAuctionDto MapToDto(ForeclosureAuction fa, string baseUrl = "", int? imageWidth = null) => new()
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
        // Altbestand enthaelt dasselbe Bild doppelt (Direktlink vs. Thumbnail-abgeleitete URL,
        // nur Gross-/Kleinschreibung unterschiedlich) - beim Ausliefern case-insensitive dedupen
        ImageUrls = GetPropertiesHandler.ProxyImageUrls(
            fa.ImageUrls.Distinct(StringComparer.OrdinalIgnoreCase).ToList(), baseUrl, width: imageWidth),
        CreatedAt = fa.CreatedAt,
        ExternalId = fa.ExternalId,
        State = fa.State,
        IsActive = fa.IsActive,
        FirstSeenAt = fa.FirstSeenAt,
        LastScrapedAt = fa.LastScrapedAt,
        RemovedAt = fa.RemovedAt
    };
}
