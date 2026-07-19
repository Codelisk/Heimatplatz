using Heimatplatz.Api.Features.Properties.Contracts;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Admin.Contracts.Mediator.Requests;

/// <summary>
/// Inseratsliste fuer den Intern-Bereich - inklusive ausgeblendeter Inserate.
/// </summary>
/// <param name="Source">null = alle, "user" = Nutzer-Inserate, "foreclosure" = Zwangsversteigerungen</param>
/// <param name="Hidden">null = alle, true = nur ausgeblendete, false = nur sichtbare</param>
/// <param name="UserId">nur Inserate dieses Nutzers (Verlinkung aus der Nutzerliste)</param>
public record GetAdminPropertiesRequest(
    int Page = 0,
    int PageSize = 50,
    string? Source = null,
    bool? Hidden = null,
    string? Search = null,
    Guid? UserId = null
) : IRequest<GetAdminPropertiesResponse>;

public record GetAdminPropertiesResponse(
    List<AdminPropertyDto> Properties,
    int Total,
    int PageSize,
    int CurrentPage,
    bool HasMore
);

public record AdminPropertyDto(
    Guid Id,
    string Title,
    string Address,
    string MunicipalityName,
    string PostalCode,
    decimal Price,
    PropertyType Type,
    SellerType SellerType,
    string SellerName,
    Guid UserId,
    string? OwnerEmail,
    string? SourceName,
    string? SourceUrl,
    bool IsHidden,
    DateTimeOffset CreatedAt,
    string? ThumbnailUrl
);
