using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;

/// <summary>
/// Request to retrieve all available seller sources
/// </summary>
public record GetSellerSourcesRequest() : IRequest<GetSellerSourcesResponse>;

/// <summary>
/// DTO for a seller source entry
/// </summary>
public record SellerSourceDto(
    Guid Id,
    string Name,
    SellerType SellerType
);

/// <summary>
/// Response with the list of seller sources
/// </summary>
public record GetSellerSourcesResponse(
    List<SellerSourceDto> Sources
);
