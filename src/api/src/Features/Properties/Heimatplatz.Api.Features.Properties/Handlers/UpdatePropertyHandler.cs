using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Heimatplatz.Api;
using Heimatplatz.Api.Authorization;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Properties.Contracts;
using Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Properties.Contracts.Models.TypeSpecific;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Handlers;

/// <summary>
/// Handler for UpdatePropertyRequest - updates an existing property owned by the authenticated user
/// Using PUT / with Id in request body instead of PUT /{Id} to test if this avoids the Shiny Mediator bug
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/properties")]
public class UpdatePropertyHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<UpdatePropertyRequest, UpdatePropertyResponse>
{
    [MediatorHttpPut("/", OperationId = "UpdateProperty", AuthorizationPolicies = [AuthorizationPolicies.RequireSeller])]
    public async Task<UpdatePropertyResponse> Handle(UpdatePropertyRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        // Extract HttpContext
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext ist nicht verfuegbar");

        // Property Id comes from request (which gets it from route parameter via binding)
        var propertyId = request.Id;

        // Extract UserId from JWT Token
        var userIdClaim = httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)
            ?? throw new UnauthorizedAccessException("Benutzer-ID nicht im Token gefunden");

        if (!Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("Ungueltige Benutzer-ID im Token");
        }

        // Load existing property
        var property = await dbContext.Set<Property>()
            .FirstOrDefaultAsync(p => p.Id == propertyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Property mit ID {propertyId} nicht gefunden");

        // Validate ownership
        if (property.UserId != userId)
        {
            throw new UnauthorizedAccessException("Sie haben keine Berechtigung, diese Immobilie zu bearbeiten");
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

        // Update property fields
        property.Title = request.Title.Trim();
        property.Address = request.Address.Trim();
        property.MunicipalityId = request.MunicipalityId;
        property.Price = request.Price;
        property.Type = request.Type;
        property.SellerType = request.SellerType;
        property.SellerName = request.SellerName.Trim();
        property.Description = request.Description?.Trim();
        property.LivingAreaSquareMeters = request.LivingAreaSquareMeters;
        property.PlotAreaSquareMeters = request.PlotAreaSquareMeters;
        property.Rooms = request.Rooms;
        property.YearBuilt = request.YearBuilt;
        property.Features = request.Features ?? new List<string>();
        property.ImageUrls = request.ImageUrls ?? new List<string>();
        property.UpdatedAt = DateTimeOffset.UtcNow;

        // Update TypeSpecificData if provided
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

        await dbContext.SaveChangesAsync(cancellationToken);

        return new UpdatePropertyResponse(
            property.Id,
            property.Title,
            property.UpdatedAt
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
