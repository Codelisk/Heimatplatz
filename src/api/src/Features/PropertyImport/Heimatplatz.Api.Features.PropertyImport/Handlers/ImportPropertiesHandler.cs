using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Heimatplatz.Api;
using Heimatplatz.Api.Authorization;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Properties.Contracts;
using Heimatplatz.Api.Features.Properties.Contracts.Enums;
using Heimatplatz.Api.Features.Properties.Contracts.Models.TypeSpecific;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Heimatplatz.Api.Features.PropertyImport.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Notifications.Contracts.Events;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.PropertyImport.Handlers;

/// <summary>
/// Handler fuer den Batch-Import von Properties
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/import")]
public class ImportPropertiesHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor,
    IMediator mediator
) : IRequestHandler<ImportPropertiesRequest, ImportPropertiesResponse>
{
    [MediatorHttpPost("/properties", OperationId = "ImportProperties", AuthorizationPolicies = [AuthorizationPolicies.RequireSeller])]
    public async Task<ImportPropertiesResponse> Handle(ImportPropertiesRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        // UserId aus JWT Token extrahieren
        var userId = GetCurrentUserId();

        var results = new List<ImportResultItem>();
        var created = 0;
        var updated = 0;
        var skipped = 0;
        var failed = 0;

        foreach (var importDto in request.Properties)
        {
            try
            {
                var result = await ProcessSinglePropertyAsync(importDto, userId, request.UpdateExisting, cancellationToken);
                results.Add(result);

                switch (result.Status)
                {
                    case ImportResultStatus.Created:
                        created++;
                        break;
                    case ImportResultStatus.Updated:
                        updated++;
                        break;
                    case ImportResultStatus.Skipped:
                        skipped++;
                        break;
                    case ImportResultStatus.Failed:
                        failed++;
                        break;
                }
            }
            catch (Exception ex)
            {
                results.Add(new ImportResultItem(
                    importDto.SourceId,
                    ImportResultStatus.Failed,
                    ErrorMessage: ex.Message
                ));
                failed++;
            }
        }

        return new ImportPropertiesResponse(
            TotalReceived: request.Properties.Count,
            Created: created,
            Updated: updated,
            Skipped: skipped,
            Failed: failed,
            Results: results
        );
    }

    private Guid GetCurrentUserId()
    {
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext ist nicht verfuegbar");

        var userIdClaim = httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)
            ?? throw new UnauthorizedAccessException("Benutzer-ID nicht im Token gefunden");

        if (!Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("Ungueltige Benutzer-ID im Token");
        }

        return userId;
    }

    private async Task<ImportResultItem> ProcessSinglePropertyAsync(
        ImportPropertyDto importDto,
        Guid userId,
        bool updateExisting,
        CancellationToken cancellationToken)
    {
        // Validierung
        ValidateImportDto(importDto);

        // Suche existierende Property via SourceName + SourceId
        var existingProperty = await dbContext.Set<Property>()
            .Include(p => p.Contacts)
            .FirstOrDefaultAsync(
                p => p.SourceName == importDto.SourceName && p.SourceId == importDto.SourceId,
                cancellationToken);

        if (existingProperty != null)
        {
            if (!updateExisting)
            {
                return new ImportResultItem(
                    importDto.SourceId,
                    ImportResultStatus.Skipped,
                    PropertyId: existingProperty.Id
                );
            }

            // Update existierende Property
            UpdateProperty(existingProperty, importDto);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new ImportResultItem(
                importDto.SourceId,
                ImportResultStatus.Updated,
                PropertyId: existingProperty.Id
            );
        }

        // Neue Property erstellen
        var newProperty = CreateProperty(importDto, userId);
        dbContext.Set<Property>().Add(newProperty);
        await dbContext.SaveChangesAsync(cancellationToken);

        // PropertyCreatedEvent publishen fuer Notification-System
        var propertyCreatedEvent = new PropertyCreatedEvent(
            newProperty.Id,
            newProperty.Title,
            newProperty.City,
            newProperty.Price,
            newProperty.Type,
            newProperty.SellerType
        );
        await mediator.Publish(propertyCreatedEvent, cancellationToken);

        return new ImportResultItem(
            importDto.SourceId,
            ImportResultStatus.Created,
            PropertyId: newProperty.Id
        );
    }

    private static void ValidateImportDto(ImportPropertyDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.SourceName))
            throw new ArgumentException("SourceName ist erforderlich");

        if (string.IsNullOrWhiteSpace(dto.SourceId))
            throw new ArgumentException("SourceId ist erforderlich");

        if (string.IsNullOrWhiteSpace(dto.Title))
            throw new ArgumentException("Title ist erforderlich");

        if (string.IsNullOrWhiteSpace(dto.Address))
            throw new ArgumentException("Address ist erforderlich");

        if (string.IsNullOrWhiteSpace(dto.City))
            throw new ArgumentException("City ist erforderlich");

        if (string.IsNullOrWhiteSpace(dto.PostalCode))
            throw new ArgumentException("PostalCode ist erforderlich");

        if (dto.Price <= 0)
            throw new ArgumentException("Price muss groesser als 0 sein");

        if (string.IsNullOrWhiteSpace(dto.SellerName))
            throw new ArgumentException("SellerName ist erforderlich");

        // Typ-spezifische Validierung
        switch (dto.Type)
        {
            case PropertyType.House:
                if (!dto.LivingAreaSquareMeters.HasValue || dto.LivingAreaSquareMeters <= 0)
                    throw new ArgumentException("LivingAreaSquareMeters ist fuer House erforderlich");
                break;

            case PropertyType.Land:
                if (!dto.PlotAreaSquareMeters.HasValue || dto.PlotAreaSquareMeters <= 0)
                    throw new ArgumentException("PlotAreaSquareMeters ist fuer Land erforderlich");
                break;

            case PropertyType.Foreclosure:
                // Foreclosure erfordert zusaetzliche Daten in TypeSpecificData
                if (dto.TypeSpecificData == null || dto.TypeSpecificData.Count == 0)
                    throw new ArgumentException("TypeSpecificData ist fuer Foreclosure erforderlich");
                break;
        }
    }

    private Property CreateProperty(ImportPropertyDto dto, Guid userId)
    {
        var property = new Property
        {
            Id = Guid.NewGuid(),
            Title = dto.Title.Trim(),
            Address = dto.Address.Trim(),
            City = dto.City.Trim(),
            PostalCode = dto.PostalCode.Trim(),
            Price = dto.Price,
            Type = dto.Type,
            SellerType = dto.SellerType,
            SellerName = dto.SellerName.Trim(),
            Description = dto.Description?.Trim(),
            LivingAreaSquareMeters = dto.LivingAreaSquareMeters,
            PlotAreaSquareMeters = dto.PlotAreaSquareMeters,
            Rooms = dto.Rooms,
            YearBuilt = dto.YearBuilt,
            Features = dto.Features ?? [],
            ImageUrls = dto.ImageUrls ?? [],
            InquiryType = dto.InquiryType,
            UserId = userId,
            CreatedAt = DateTimeOffset.UtcNow,

            // Import-spezifische Felder
            SourceName = dto.SourceName,
            SourceId = dto.SourceId,
            SourceUrl = dto.SourceUrl,
            SourceLastUpdated = DateTimeOffset.UtcNow
        };

        // TypeSpecificData setzen
        SetTypeSpecificData(property, dto);

        // Kontakte hinzufuegen
        AddContacts(property, dto);

        return property;
    }

    private void UpdateProperty(Property property, ImportPropertyDto dto)
    {
        property.Title = dto.Title.Trim();
        property.Address = dto.Address.Trim();
        property.City = dto.City.Trim();
        property.PostalCode = dto.PostalCode.Trim();
        property.Price = dto.Price;
        property.Type = dto.Type;
        property.SellerType = dto.SellerType;
        property.SellerName = dto.SellerName.Trim();
        property.Description = dto.Description?.Trim();
        property.LivingAreaSquareMeters = dto.LivingAreaSquareMeters;
        property.PlotAreaSquareMeters = dto.PlotAreaSquareMeters;
        property.Rooms = dto.Rooms;
        property.YearBuilt = dto.YearBuilt;
        property.Features = dto.Features ?? [];
        property.ImageUrls = dto.ImageUrls ?? [];
        property.InquiryType = dto.InquiryType;
        property.UpdatedAt = DateTimeOffset.UtcNow;
        property.SourceUrl = dto.SourceUrl;
        property.SourceLastUpdated = DateTimeOffset.UtcNow;

        // TypeSpecificData aktualisieren
        SetTypeSpecificData(property, dto);

        // Kontakte aktualisieren: Alte loeschen, neue hinzufuegen
        dbContext.Set<PropertyContactInfo>().RemoveRange(property.Contacts);
        property.Contacts.Clear();
        AddContacts(property, dto);
    }

    private static void SetTypeSpecificData(Property property, ImportPropertyDto dto)
    {
        if (dto.TypeSpecificData == null || dto.TypeSpecificData.Count == 0)
        {
            property.TypeSpecificData = "{}";
            return;
        }

        var json = JsonSerializer.Serialize(dto.TypeSpecificData);

        switch (dto.Type)
        {
            case PropertyType.Land:
                var landData = JsonSerializer.Deserialize<LandPropertyData>(json);
                if (landData != null) property.SetTypedData(landData);
                break;

            case PropertyType.House:
                var houseData = JsonSerializer.Deserialize<HousePropertyData>(json);
                if (houseData != null) property.SetTypedData(houseData);
                break;

            case PropertyType.Foreclosure:
                var foreclosureData = JsonSerializer.Deserialize<ForeclosurePropertyData>(json);
                if (foreclosureData != null) property.SetTypedData(foreclosureData);
                break;

            default:
                property.TypeSpecificData = json;
                break;
        }
    }

    private static void AddContacts(Property property, ImportPropertyDto dto)
    {
        if (dto.Contacts == null || dto.Contacts.Count == 0)
            return;

        var displayOrder = 0;
        foreach (var contactDto in dto.Contacts)
        {
            var contact = new PropertyContactInfo
            {
                Id = Guid.NewGuid(),
                PropertyId = property.Id,
                Type = contactDto.Type,
                Source = ContactSource.Import,
                Name = contactDto.Name,
                Email = contactDto.Email,
                Phone = contactDto.Phone,
                SourceName = dto.SourceName,
                SourceId = dto.SourceId,
                OriginalListingUrl = dto.SourceUrl,
                DisplayOrder = displayOrder++,
                CreatedAt = DateTimeOffset.UtcNow
            };

            property.Contacts.Add(contact);
        }
    }
}
