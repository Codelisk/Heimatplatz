using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;

/// <summary>
/// Request to delete an existing property
/// </summary>
public record DeletePropertyRequest(Guid Id) : IRequest<DeletePropertyResponse>;

/// <summary>
/// Response after successful property deletion
/// </summary>
public record DeletePropertyResponse(
    bool Success,
    string Message
);
