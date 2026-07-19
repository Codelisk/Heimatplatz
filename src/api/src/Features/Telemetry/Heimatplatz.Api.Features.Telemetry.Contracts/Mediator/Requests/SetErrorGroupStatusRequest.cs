using Heimatplatz.Api.Features.Telemetry.Contracts.Mediator.Models;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Telemetry.Contracts.Mediator.Requests;

/// <summary>
/// Setzt den Triage-Status einer Fehlergruppe (Open/Resolved/Ignored).
/// </summary>
public record SetErrorGroupStatusRequest(
    Guid Id,
    ErrorGroupStatus Status
) : IRequest<SetErrorGroupStatusResponse>;

/// <summary>
/// Bestaetigung mit dem neuen Status.
/// </summary>
public record SetErrorGroupStatusResponse(
    Guid Id,
    ErrorGroupStatus Status
);
