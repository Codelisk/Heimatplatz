using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;

/// <summary>
/// Request zum Abrufen einer einzelnen Immobilie
/// </summary>
public record GetPropertyByIdRequest(Guid Id) : IRequest<GetPropertyByIdResponse>;

/// <summary>
/// Response fuer GetPropertyByIdRequest
/// </summary>
/// <param name="Property">Die gefundene Immobilie (null wenn nicht gefunden)</param>
public record GetPropertyByIdResponse(PropertyDto? Property);
