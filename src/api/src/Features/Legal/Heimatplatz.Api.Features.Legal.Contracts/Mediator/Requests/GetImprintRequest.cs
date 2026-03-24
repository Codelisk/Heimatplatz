using Heimatplatz.Api.Features.Legal.Contracts.Models;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Legal.Contracts.Mediator.Requests;

/// <summary>
/// Request zum Abrufen des aktuellen Impressums
/// </summary>
public record GetImprintRequest : IRequest<GetImprintResponse>;

/// <summary>
/// Response mit dem Impressum
/// </summary>
public record GetImprintResponse(ImprintDto? Imprint);
