using System.IdentityModel.Tokens.Jwt;
using Heimatplatz.Api;
using Heimatplatz.Api.Authorization;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Properties.Contracts;
using Heimatplatz.Api.Features.Properties.Contracts.Enums;
using Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Properties.Contracts.Models.TypeSpecific;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Handlers;

/// <summary>
/// Handler for GetPropertiesRequest - returns a filtered list of properties.
/// Blocked properties are automatically excluded for authenticated users.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/properties")]
public class GetPropertiesHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration
) : IRequestHandler<GetPropertiesRequest, GetPropertiesResponse>
{
    // Obergrenze pro Seite: schuetzt vor PageSize=1000000-Anfragen. Groesster
    // legitimer Abnehmer ist das Astro-Web mit 96 Eintraegen pro Ladung.
    private const int MaxPageSize = 200;

    [MediatorHttpGet("/", OperationId = "GetProperties")]
    public async Task<GetPropertiesResponse> Handle(GetPropertiesRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        // Include Municipality for City/PostalCode values.
        // Admin-seitig ausgeblendete Inserate (IsHidden) sind oeffentlich unsichtbar.
        var query = dbContext.Set<Property>()
            .Include(p => p.Municipality)
            .AsNoTracking()
            .Where(p => !p.IsHidden);

        // Exclude blocked properties for authenticated users
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                var blockedPropertyIds = dbContext.Set<Blocked>()
                    .Where(b => b.UserId == userId)
                    .Select(b => b.PropertyId);

                query = query.Where(p => !blockedPropertyIds.Contains(p.Id));
            }
        }

        // Filter: PropertyTypes (Multi-Select)
        var propertyTypes = request.GetPropertyTypes();
        if (propertyTypes.Count > 0)
            query = query.Where(p => propertyTypes.Contains(p.Type));

        // Filter: SellerTypes (Multi-Select)
        var sellerTypes = request.GetSellerTypes();
        if (sellerTypes.Count > 0)
            query = query.Where(p => sellerTypes.Contains(p.SellerType));

        // Filter: MunicipalityIds (Multi-Select)
        var municipalityIds = request.GetMunicipalityIds();
        if (municipalityIds.Count > 0)
            query = query.Where(p => municipalityIds.Contains(p.MunicipalityId));

        // Filter: CreatedAfter (Age filter) - convert to UTC DateTimeOffset for proper comparison.
        // Laeuft auch auf SQLite in SQL - die Konverter im AppDbContext speichern
        // DateTimeOffset dort als long (UTC-Ticks), kein In-Memory-Umweg noetig.
        if (request.CreatedAfter.HasValue)
        {
            var createdAfterUtc = new DateTimeOffset(request.CreatedAfter.Value.ToUniversalTime(), TimeSpan.Zero);
            query = query.Where(p => p.CreatedAt >= createdAfterUtc);
        }

        // Filter: Price range
        if (request.PriceMin.HasValue)
            query = query.Where(p => p.Price >= request.PriceMin.Value);

        if (request.PriceMax.HasValue)
            query = query.Where(p => p.Price <= request.PriceMax.Value);

        // Filter: Area range (use LivingArea if available, otherwise PlotArea)
        if (request.AreaMin.HasValue)
            query = query.Where(p => (p.LivingAreaSquareMeters ?? p.PlotAreaSquareMeters) >= request.AreaMin.Value);

        if (request.AreaMax.HasValue)
            query = query.Where(p => (p.LivingAreaSquareMeters ?? p.PlotAreaSquareMeters) <= request.AreaMax.Value);

        // Filter: Minimum rooms
        if (request.RoomsMin.HasValue)
            query = query.Where(p => p.Rooms >= request.RoomsMin.Value);

        // Filter: Volltextsuche - serverseitig, damit Clients nicht ueber den geladenen
        // Seiten-Ausschnitt suchen muessen (ToLower().Contains uebersetzt auf allen Providern)
        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            var search = request.SearchText.Trim().ToLower();
            query = query.Where(p =>
                p.Title.ToLower().Contains(search) ||
                (p.Description ?? "").ToLower().Contains(search) ||
                p.Address.ToLower().Contains(search) ||
                p.Municipality.Name.ToLower().Contains(search));
        }

        // Filter: Bestimmte Anbieter (Makler/Verwaltungen) ausblenden - ueber die
        // SellerSource-FK-Beziehung statt fragilem String-Matching auf SellerName
        var excludedSourceIds = request.GetExcludedSellerSourceIds();
        if (excludedSourceIds.Count > 0)
        {
            query = query.Where(p =>
                p.SellerSourceId == null ||
                !excludedSourceIds.Contains(p.SellerSourceId.Value));
        }

        var page = Math.Max(request.Page, 0);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        // Total count for paging (before applying pagination)
        var total = await query.CountAsync(cancellationToken);
        var hasMore = (page + 1) * pageSize < total;

        // Build base URL for image proxy
        var baseUrl = ResolveApiBaseUrl(httpContextAccessor, configuration);

        // Sortierung + Paging in der Datenbank: nur die angeforderte Seite wird geladen.
        // (SQLite-DateTimeOffset/decimal-ORDER-BY laeuft ueber die Konverter im AppDbContext.)
        var sorted = request.SortBy?.ToLowerInvariant() switch
        {
            "createdat" => request.SortDescending
                ? query.OrderByDescending(p => p.CreatedAt)
                : query.OrderBy(p => p.CreatedAt),
            "price" => request.SortDescending
                ? query.OrderByDescending(p => p.Price)
                : query.OrderBy(p => p.Price),
            "plotarea" => request.SortDescending
                ? query.OrderByDescending(p => p.PlotAreaSquareMeters ?? 0)
                : query.OrderBy(p => p.PlotAreaSquareMeters ?? 0),
            "postalcode" => request.SortDescending
                ? query.OrderByDescending(p => p.PostalCode ?? p.Municipality.PostalCode)
                : query.OrderBy(p => p.PostalCode ?? p.Municipality.PostalCode),
            _ => query.OrderByDescending(p => p.CreatedAt) // default: newest first
        };

        var entities = await sorted
            .ThenBy(p => p.Id) // stabiler Tiebreaker: deterministisches Paging bei gleichen Sortierwerten
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var properties = entities
            .Select(p => new PropertyListItemDto(
                p.Id,
                p.Title,
                p.Address,
                p.MunicipalityId,
                p.Municipality.Name,
                p.PostalCode ?? p.Municipality.PostalCode,
                p.Price,
                p.LivingAreaSquareMeters,
                p.PlotAreaSquareMeters,
                p.Rooms,
                p.Type,
                p.SellerType,
                p.SellerName,
                ProxyImageUrls(p.ImageUrls, baseUrl, width: ListThumbnailWidth),
                p.CreatedAt,
                p.InquiryType,
                p.SourceName,
                ResolveAuctionDate(p)
            ))
            .ToList();

        return new GetPropertiesResponse(
            properties,
            total,
            pageSize,
            page,
            hasMore
        );
    }

    // Zielbreite fuer Listen-Thumbnails (Karten sind min. 320dp breit, 640px deckt 2x-Displays ab
    // ohne die haeufig mehrere MB grossen Originalbilder ungeskaliert an den Client zu schicken).
    public const int ListThumbnailWidth = 640;

    /// <summary>
    /// Auktionstermin fuer Listen-Karten (WEB-011): nur bei Zwangsversteigerungen belegt,
    /// damit Karten den Termin statt des Einstelldatums beschriften koennen.
    /// Muss in-memory laufen (TypeSpecificData-JSON) - nicht in EF-Projektionen verwenden.
    /// </summary>
    public static DateTime? ResolveAuctionDate(Property property)
    {
        if (property.Type != PropertyType.Foreclosure)
            return null;

        // default(DateTime) aus unvollstaendigem JSON nicht als echten Termin ausliefern
        var auctionDate = property.GetTypedData<ForeclosurePropertyData>()?.AuctionDate;
        return auctionDate is { Year: > 1900 } ? auctionDate : null;
    }

    /// <summary>
    /// Basis-URL fuer absolute, browser-erreichbare Links (Bild-Proxy etc.). Nutzt bevorzugt die
    /// konfigurierte oeffentliche URL (Api:PublicBaseUrl) statt Scheme+Host der eingehenden Anfrage -
    /// SSR-Server (Astro-Web) rufen die API teils direkt ueber das interne Docker-Netz
    /// (http://api:8080) auf, dessen Host fuer echte Browser nicht erreichbar ist. Ohne konfigurierte
    /// PublicBaseUrl (z.B. lokale Entwicklung) faellt die Methode auf Scheme+Host der Anfrage zurueck.
    /// </summary>
    public static string ResolveApiBaseUrl(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    {
        var req = httpContextAccessor.HttpContext?.Request;
        var configured = configuration["Api:PublicBaseUrl"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            var normalizedConfigured = configured.TrimEnd('/');

            // In der lokalen Entwicklung ist PublicBaseUrl oft "localhost",
            // Android erreicht dieselbe API aber ueber 10.0.2.2. In diesem
            // Sonderfall muss die Antwort den vom Client verwendeten Origin
            // spiegeln; echte oeffentliche Produktions-URLs behalten weiterhin
            // Vorrang vor internen Docker-Hosts.
            if (req is not null &&
                Uri.TryCreate(normalizedConfigured, UriKind.Absolute, out var configuredUri) &&
                configuredUri.IsLoopback)
            {
                var requestOrigin = $"{req.Scheme}://{req.Host}";
                if (Uri.TryCreate(requestOrigin, UriKind.Absolute, out var requestUri) &&
                    !requestUri.IsLoopback)
                {
                    return requestOrigin.TrimEnd('/');
                }
            }

            return normalizedConfigured;
        }

        return req != null ? $"{req.Scheme}://{req.Host}" : "";
    }

    public static List<string> ProxyImageUrls(List<string> urls, string baseUrl, int? width = null)
    {
        if (urls.Count == 0 || string.IsNullOrEmpty(baseUrl))
            return urls;

        return urls.Select(url =>
        {
            if (string.IsNullOrEmpty(url))
                return url;

            // Lokale Uploads als absolute API-URL ausliefern. Das Astro-Frontend
            // laeuft auf einer separaten Origin; ein nacktes /uploads/... wuerde
            // sonst irrtuemlich gegen heimatplatz.at statt api.heimatplatz.at laden.
            // Mit Zielbreite (Listen-Thumbnails) ueber den skalierenden Endpoint statt
            // als statische Datei - die Display-Varianten sind bis zu 2560px breit.
            if (url.StartsWith("/"))
                return BuildLocalImageUrl(baseUrl, url, width);

            // Externe Quellen ueber den abgesicherten Bild-Proxy ausliefern.

            if (Uri.TryCreate(url, UriKind.Absolute, out var parsed)
                && (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps))
            {
                // Eigene Uploads (gleicher Host wie die API, z.B. via UploadListingMedia
                // absolut gespeichert) sind lokale statische Dateien - direkt bzw. mit
                // Zielbreite ueber den skalierenden Endpoint ausliefern. Der Proxy kennt
                // nur externe Bild-Hosts und wuerde den eigenen Host mit 400 ablehnen.
                // Loopback-Hosts ebenfalls rebasen: Bild-URLs werden absolut mit dem
                // Upload-Request-Host gespeichert - gegen eine lokale Dev-API entstandene
                // "http://localhost:xxxx/uploads/..."-Altlasten waeren im Browser IMMER
                // kaputt (Mixed Content + CSP), lokal liegt die Datei aber meist vor.
                var hasBaseUri = Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri);
                var isSameOrigin = hasBaseUri
                    && string.Equals(parsed.Scheme, baseUri!.Scheme, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(parsed.Host, baseUri.Host, StringComparison.OrdinalIgnoreCase)
                    && parsed.Port == baseUri.Port;

                // Seed- und Upload-Altbestände können eine localhost-URL enthalten.
                // Auf Android bezeichnet localhost jedoch den Emulator selbst. Gegen
                // den Origin der aktuellen API-Anfrage rebasen und Query beibehalten.
                if (parsed.IsLoopback && hasBaseUri && !isSameOrigin)
                {
                    return parsed.AbsolutePath.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase)
                        ? BuildLocalImageUrl(baseUrl, parsed.AbsolutePath, width)
                        : $"{baseUrl}{parsed.PathAndQuery}";
                }

                if (isSameOrigin)
                {
                    return parsed.AbsolutePath.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase)
                        ? BuildLocalImageUrl(baseUrl, parsed.AbsolutePath, width)
                        : url;
                }

                if (parsed.Scheme == Uri.UriSchemeHttps)
                {
                    var proxied = $"{baseUrl}/api/images/proxy?url={Uri.EscapeDataString(url)}";
                    return width.HasValue ? $"{proxied}&w={width.Value}" : proxied;
                }
            }

            return url;
        }).ToList();
    }

    /// <summary>
    /// URL eines lokalen Uploads: ohne Zielbreite direkt als statische Datei (Detailseiten,
    /// volle Display-Variante), mit Zielbreite ueber den skalierenden /api/images/local-Endpoint
    /// (Listen-Thumbnails, serverseitig gecacht).
    /// </summary>
    private static string BuildLocalImageUrl(string baseUrl, string path, int? width)
        => width.HasValue
            ? $"{baseUrl}/api/images/local?path={Uri.EscapeDataString(path)}&w={width.Value}"
            : $"{baseUrl}{path}";
}
