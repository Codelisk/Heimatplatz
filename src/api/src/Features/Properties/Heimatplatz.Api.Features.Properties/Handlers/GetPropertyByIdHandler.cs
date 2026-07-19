using System.Text.Json;
using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Properties.Contracts;
using Heimatplatz.Api.Features.Properties.Contracts.Enums;
using Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Properties.Contracts.Models.TypeSpecific;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Handlers;

/// <summary>
/// Handler fuer GetPropertyByIdRequest - gibt einzelne Immobilie zurueck
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/properties")]
public class GetPropertyByIdHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration
) : IRequestHandler<GetPropertyByIdRequest, GetPropertyByIdResponse>
{
    [MediatorHttpGet("/{Id}", OperationId = "GetPropertyById")]
    public async Task<GetPropertyByIdResponse> Handle(GetPropertyByIdRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        // IsHidden: moderierte Inserate liefern wie geloeschte ein leeres Ergebnis (404-Verhalten)
        var property = await dbContext.Set<Property>()
            .Include(p => p.Municipality)
            .Where(p => p.Id == request.Id && !p.IsHidden)
            .Select(p => new PropertyDto(
                p.Id,
                p.Title,
                p.Address,
                p.MunicipalityId,
                p.Municipality.Name,
                p.Municipality.PostalCode,
                p.Price,
                p.LivingAreaSquareMeters,
                p.PlotAreaSquareMeters,
                p.Rooms,
                p.YearBuilt,
                p.Type,
                p.SellerType,
                p.SellerName,
                p.Description,
                p.Features,
                p.ImageUrls,
                p.CreatedAt,
                p.InquiryType,
                p.Contacts
                    .OrderBy(c => c.DisplayOrder)
                    .Select(c => new ContactInfoDto(
                        c.Id,
                        c.Type,
                        c.Source,
                        c.Name,
                        c.Email,
                        c.Phone,
                        c.OriginalListingUrl,
                        c.SourceName,
                        c.DisplayOrder
                    ))
                    .ToList(),
                p.TypeSpecificData
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (property != null && property.ImageUrls.Count > 0)
        {
            var baseUrl = GetPropertiesHandler.ResolveApiBaseUrl(httpContextAccessor, configuration);
            var proxied = GetPropertiesHandler.ProxyImageUrls(property.ImageUrls, baseUrl);
            property = property with { ImageUrls = proxied };
        }

        if (property != null)
        {
            // Abgeleitete Anzeige-Fakten serverseitig berechnen (Backend-First) -
            // die Clients muessen TypeSpecificData dafuer nicht mehr interpretieren
            property = property with
            {
                PriceLabel = ComputePriceLabel(property.Type, property.TypeSpecificData),
                PricePerSquareMeter = ComputePricePerSquareMeter(property)
            };
        }

        return new GetPropertyByIdResponse(property);
    }

    private static readonly JsonSerializerOptions TypeSpecificJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Bei Zwangsversteigerungen ist Price = MinimumBid ?? EstimatedValue (Sync):
    /// ohne Mindestgebot ist der Wert der Schaetzwert, sonst das Mindestgebot.
    /// Normale Inserate: Kaufpreis.
    /// </summary>
    private static string ComputePriceLabel(PropertyType type, string? typeSpecificData)
    {
        if (type != PropertyType.Foreclosure)
            return "Kaufpreis";

        if (!string.IsNullOrWhiteSpace(typeSpecificData))
        {
            try
            {
                var data = JsonSerializer.Deserialize<ForeclosurePropertyData>(typeSpecificData, TypeSpecificJsonOptions);
                if (data is { MinimumBid: <= 0, EstimatedValue: > 0 })
                    return "Schätzwert";
            }
            catch (JsonException)
            {
                // Unlesbares TypeSpecificData: konservativ als Mindestgebot ausweisen
            }
        }

        return "Mindestgebot";
    }

    /// <summary>
    /// Preis pro m² Wohnflaeche - nicht fuer Zwangsversteigerungen (dort waere es
    /// Mindestgebot / bebaute Flaeche, kein Kaufpreis pro m²).
    /// </summary>
    private static decimal? ComputePricePerSquareMeter(PropertyDto property) =>
        property.Type != PropertyType.Foreclosure && property.Price > 0 && property.LivingAreaM2 is > 0
            ? Math.Round(property.Price / property.LivingAreaM2.Value, 2)
            : null;
}
