using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Properties.Contracts;
using Heimatplatz.Api.Features.Properties.Contracts.Enums;
using Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Handlers;

/// <summary>
/// Handler fuer GetPropertyByIdRequest - gibt einzelne Immobilie zurueck
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/properties")]
public class GetPropertyByIdHandler(AppDbContext dbContext) : IRequestHandler<GetPropertyByIdRequest, GetPropertyByIdResponse>
{
    [MediatorHttpGet("/{Id}", OperationId = "GetPropertyById")]
    public async Task<GetPropertyByIdResponse> Handle(GetPropertyByIdRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        var property = await dbContext.Set<Property>()
            .Where(p => p.Id == request.Id)
            .Select(p => new PropertyDto(
                p.Id,
                p.Title,
                p.Address,
                p.City,
                p.PostalCode,
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
                    .ToList()
            ))
            .FirstOrDefaultAsync(cancellationToken);

        return new GetPropertyByIdResponse(property);
    }
}
