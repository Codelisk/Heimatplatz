using System.IdentityModel.Tokens.Jwt;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Properties.Contracts;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.AspNetCore.Http;

namespace Heimatplatz.Api.Features.Properties.Services;

/// <summary>
/// Gemeinsame Filterlogik der Immobilien-Suche. Wird von GetPropertiesHandler
/// (Trefferliste) und GetPropertyMapPinsHandler (Kartenansicht) verwendet, damit
/// Karte und Liste bei identischen Parametern garantiert dieselbe Treffermenge
/// zeigen - Aenderungen hier wirken bewusst auf beide.
/// </summary>
public static class PropertyQueryFilters
{
    /// <summary>
    /// Blockierte Immobilien fuer den angemeldeten Benutzer ausblenden.
    /// Anonyme Requests bleiben unveraendert.
    /// </summary>
    public static IQueryable<Property> ExcludeBlockedForCurrentUser(
        IQueryable<Property> query,
        AppDbContext dbContext,
        IHttpContextAccessor httpContextAccessor)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
            return query;

        var userIdClaim = httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return query;

        var blockedPropertyIds = dbContext.Set<Blocked>()
            .Where(b => b.UserId == userId)
            .Select(b => b.PropertyId);

        return query.Where(p => !blockedPropertyIds.Contains(p.Id));
    }

    /// <summary>
    /// Wendet alle gemeinsamen Suchfilter an (Typ, Anbieter, Gemeinden, Alter,
    /// Preis, Flaeche, Zimmer, Volltext, ausgeschlossene Anbieter-Quellen).
    /// </summary>
    public static IQueryable<Property> ApplyCommonFilters(
        IQueryable<Property> query,
        List<PropertyType> propertyTypes,
        List<SellerType> sellerTypes,
        List<Guid> municipalityIds,
        DateTime? createdAfter,
        decimal? priceMin,
        decimal? priceMax,
        int? areaMin,
        int? areaMax,
        int? roomsMin,
        string? searchText,
        List<Guid> excludedSellerSourceIds)
    {
        if (propertyTypes.Count > 0)
            query = query.Where(p => propertyTypes.Contains(p.Type));

        if (sellerTypes.Count > 0)
            query = query.Where(p => sellerTypes.Contains(p.SellerType));

        if (municipalityIds.Count > 0)
            query = query.Where(p => municipalityIds.Contains(p.MunicipalityId));

        // CreatedAfter in UTC-DateTimeOffset umrechnen - laeuft auch auf SQLite in SQL,
        // die Konverter im AppDbContext speichern DateTimeOffset dort als long (UTC-Ticks)
        if (createdAfter.HasValue)
        {
            var createdAfterUtc = new DateTimeOffset(createdAfter.Value.ToUniversalTime(), TimeSpan.Zero);
            query = query.Where(p => p.CreatedAt >= createdAfterUtc);
        }

        if (priceMin.HasValue)
            query = query.Where(p => p.Price >= priceMin.Value);

        if (priceMax.HasValue)
            query = query.Where(p => p.Price <= priceMax.Value);

        // Flaeche: LivingArea bevorzugt, sonst PlotArea
        if (areaMin.HasValue)
            query = query.Where(p => (p.LivingAreaSquareMeters ?? p.PlotAreaSquareMeters) >= areaMin.Value);

        if (areaMax.HasValue)
            query = query.Where(p => (p.LivingAreaSquareMeters ?? p.PlotAreaSquareMeters) <= areaMax.Value);

        if (roomsMin.HasValue)
            query = query.Where(p => p.Rooms >= roomsMin.Value);

        // Volltextsuche serverseitig (ToLower().Contains uebersetzt auf allen Providern)
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var search = searchText.Trim().ToLower();
            query = query.Where(p =>
                p.Title.ToLower().Contains(search) ||
                (p.Description ?? "").ToLower().Contains(search) ||
                p.Address.ToLower().Contains(search) ||
                p.Municipality.Name.ToLower().Contains(search));
        }

        // Anbieter-Ausschluss ueber die SellerSource-FK-Beziehung
        if (excludedSellerSourceIds.Count > 0)
        {
            query = query.Where(p =>
                p.SellerSourceId == null ||
                !excludedSellerSourceIds.Contains(p.SellerSourceId.Value));
        }

        return query;
    }
}
