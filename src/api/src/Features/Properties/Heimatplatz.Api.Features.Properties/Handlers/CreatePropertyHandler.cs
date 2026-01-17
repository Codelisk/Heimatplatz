using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Heimatplatz.Api;
using Heimatplatz.Api.Authorization;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Properties.Contracts;
using Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Properties.Contracts.Models.TypeSpecific;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.AspNetCore.Http;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Handlers;

/// <summary>
/// Handler zum Erstellen einer neuen Immobilie
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/properties")]
public class CreatePropertyHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<CreatePropertyRequest, CreatePropertyResponse>
{
    [MediatorHttpPost("/", OperationId = "CreateProperty", AuthorizationPolicies = [AuthorizationPolicies.RequireSeller])]
    public async Task<CreatePropertyResponse> Handle(CreatePropertyRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        // UserId aus JWT Token extrahieren
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext ist nicht verfuegbar");

        var userIdClaim = httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)
            ?? throw new UnauthorizedAccessException("Benutzer-ID nicht im Token gefunden");

        if (!Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("Ungueltige Benutzer-ID im Token");
        }

        // Validation
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length < 10 || request.Title.Length > 200)
        {
            throw new ArgumentException("Title must be between 10 and 200 characters", nameof(request.Title));
        }

        if (request.Price <= 0 || request.Price > 100_000_000)
        {
            throw new ArgumentException("Price must be greater than 0 and less than 100,000,000", nameof(request.Price));
        }

        if (string.IsNullOrWhiteSpace(request.Description) || request.Description.Length < 50 || request.Description.Length > 2000)
        {
            throw new ArgumentException("Description must be between 50 and 2000 characters", nameof(request.Description));
        }

        // Create property
        var property = new Property
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Address = request.Address.Trim(),
            City = request.City.Trim(),
            PostalCode = request.PostalCode.Trim(),
            Price = request.Price,
            Type = request.Type,
            SellerType = request.SellerType,
            SellerName = request.SellerName.Trim(),
            Description = request.Description?.Trim(),
            LivingAreaSquareMeters = request.LivingAreaSquareMeters,
            PlotAreaSquareMeters = request.PlotAreaSquareMeters,
            Rooms = request.Rooms,
            YearBuilt = request.YearBuilt,
            Features = request.Features ?? new List<string>(),
            ImageUrls = request.ImageUrls ?? new List<string>(),
            UserId = userId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // TypeSpecificData serialisieren und validieren basierend auf PropertyType
        if (request.TypeSpecificData != null && request.TypeSpecificData.Count > 0)
        {
            var typeSpecificDataJson = JsonSerializer.Serialize(request.TypeSpecificData);

            // Deserialize and validate based on PropertyType
            switch (request.Type)
            {
                case PropertyType.Land:
                    var landData = JsonSerializer.Deserialize<LandPropertyData>(typeSpecificDataJson);
                    if (landData != null)
                    {
                        ValidateLandData(landData);
                        property.SetTypedData(landData);
                    }
                    break;

                case PropertyType.House:
                    var houseData = JsonSerializer.Deserialize<HousePropertyData>(typeSpecificDataJson);
                    if (houseData != null)
                    {
                        ValidateHouseData(houseData);
                        property.SetTypedData(houseData);
                    }
                    break;

                case PropertyType.Foreclosure:
                    var foreclosureData = JsonSerializer.Deserialize<ForeclosurePropertyData>(typeSpecificDataJson);
                    if (foreclosureData != null)
                    {
                        ValidateForeclosureData(foreclosureData);
                        property.SetTypedData(foreclosureData);
                    }
                    break;
            }
        }

        dbContext.Set<Property>().Add(property);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreatePropertyResponse(
            property.Id,
            property.Title,
            property.CreatedAt
        );
    }

    /// <summary>
    /// Validates land-specific data
    /// </summary>
    private static void ValidateLandData(LandPropertyData data)
    {
        if (data.PlotSizeInSquareMeters <= 0)
        {
            throw new ArgumentException("Plot size must be greater than 0", nameof(data.PlotSizeInSquareMeters));
        }

        if (data.PlotSizeInSquareMeters > 1_000_000)
        {
            throw new ArgumentException("Plot size is unrealistically large", nameof(data.PlotSizeInSquareMeters));
        }
    }

    /// <summary>
    /// Validates house-specific data
    /// </summary>
    private static void ValidateHouseData(HousePropertyData data)
    {
        if (data.LivingAreaInSquareMeters <= 0)
        {
            throw new ArgumentException("Living area must be greater than 0", nameof(data.LivingAreaInSquareMeters));
        }

        if (data.LivingAreaInSquareMeters > 10_000)
        {
            throw new ArgumentException("Living area is unrealistically large", nameof(data.LivingAreaInSquareMeters));
        }

        if (data.TotalRooms <= 0)
        {
            throw new ArgumentException("Total rooms must be at least 1", nameof(data.TotalRooms));
        }

        if (data.Bedrooms < 0 || data.Bedrooms > data.TotalRooms)
        {
            throw new ArgumentException("Bedrooms must be between 0 and total rooms", nameof(data.Bedrooms));
        }

        if (data.Bathrooms <= 0)
        {
            throw new ArgumentException("Bathrooms must be at least 1", nameof(data.Bathrooms));
        }

        if (data.YearBuilt.HasValue && (data.YearBuilt < 1800 || data.YearBuilt > DateTime.Now.Year + 5))
        {
            throw new ArgumentException("Year built is not valid", nameof(data.YearBuilt));
        }

        if (data.Floors <= 0)
        {
            throw new ArgumentException("Floors must be at least 1", nameof(data.Floors));
        }
    }

    /// <summary>
    /// Validates foreclosure-specific data
    /// </summary>
    private static void ValidateForeclosureData(ForeclosurePropertyData data)
    {
        if (string.IsNullOrWhiteSpace(data.CourtName))
        {
            throw new ArgumentException("Court name is required", nameof(data.CourtName));
        }

        if (data.AuctionDate < DateTime.Now.AddDays(-30))
        {
            throw new ArgumentException("Auction date cannot be more than 30 days in the past", nameof(data.AuctionDate));
        }

        if (data.MinimumBid <= 0)
        {
            throw new ArgumentException("Minimum bid must be greater than 0", nameof(data.MinimumBid));
        }

        if (string.IsNullOrWhiteSpace(data.FileNumber))
        {
            throw new ArgumentException("File number is required", nameof(data.FileNumber));
        }
    }
}
