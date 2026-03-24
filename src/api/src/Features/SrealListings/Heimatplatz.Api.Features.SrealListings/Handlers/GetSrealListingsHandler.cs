using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.SrealListings.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.SrealListings.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.SrealListings.Handlers;

[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/sreal-listings")]
public class GetSrealListingsHandler(AppDbContext dbContext)
    : IRequestHandler<GetSrealListingsRequest, GetSrealListingsResponse>
{
    [MediatorHttpGet("/", OperationId = "GetSrealListings")]
    public async Task<GetSrealListingsResponse> Handle(
        GetSrealListingsRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Set<SrealListing>().AsQueryable();

        // Filter anwenden
        if (request.ObjectType.HasValue)
            query = query.Where(s => s.ObjectType == request.ObjectType.Value);

        if (!string.IsNullOrWhiteSpace(request.City))
            query = query.Where(s => s.City.Contains(request.City));

        if (!string.IsNullOrWhiteSpace(request.PostalCode))
            query = query.Where(s => s.PostalCode.StartsWith(request.PostalCode));

        if (request.MaxPrice.HasValue)
            query = query.Where(s => s.Price <= request.MaxPrice.Value);

        if (request.State.HasValue)
            query = query.Where(s => s.State == request.State.Value);

        if (request.IsActive.HasValue)
            query = query.Where(s => s.IsActive == request.IsActive.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        // Laden und sortieren (SQLite DateTimeOffset workaround)
        var entities = await query.ToListAsync(cancellationToken);
        var listings = entities
            .OrderByDescending(s => s.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(MapToDto)
            .ToList();

        return new GetSrealListingsResponse
        {
            Listings = listings,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    internal static SrealListingDto MapToDto(SrealListing s) => new()
    {
        Id = s.Id,
        ExternalId = s.ExternalId,
        Title = s.Title,
        Address = s.Address,
        City = s.City,
        PostalCode = s.PostalCode,
        District = s.District,
        State = s.State,
        ObjectType = s.ObjectType,
        BuyingType = s.BuyingType,
        Price = s.Price,
        PriceText = s.PriceText,
        Commission = s.Commission,
        LivingArea = s.LivingArea,
        PlotArea = s.PlotArea,
        Rooms = s.Rooms,
        Description = s.Description,
        EnergyClass = s.EnergyClass,
        EnergyValue = s.EnergyValue,
        AgentName = s.AgentName,
        AgentPhone = s.AgentPhone,
        AgentEmail = s.AgentEmail,
        AgentOffice = s.AgentOffice,
        ImageUrls = s.ImageUrls,
        SourceUrl = s.SourceUrl,
        IsActive = s.IsActive,
        FirstSeenAt = s.FirstSeenAt,
        LastScrapedAt = s.LastScrapedAt,
        RemovedAt = s.RemovedAt,
        CreatedAt = s.CreatedAt
    };
}
