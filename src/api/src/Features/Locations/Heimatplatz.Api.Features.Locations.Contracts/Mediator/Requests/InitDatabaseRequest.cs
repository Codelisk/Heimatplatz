using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Locations.Contracts.Mediator.Requests;

public record InitDatabaseRequest : IRequest<InitDatabaseResponse>;

public record InitDatabaseResponse(bool Success, string Message);
