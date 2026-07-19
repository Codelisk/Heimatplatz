using Heimatplatz.Api;
using Heimatplatz.Api.Authorization;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Exceptions;
using Heimatplatz.Api.Features.Telemetry.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Telemetry.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Telemetry.Handlers;

/// <summary>
/// Setzt den Triage-Status einer Fehlergruppe (Open/Resolved/Ignored).
/// Tritt eine Resolved-Gruppe erneut auf, bleibt der Status bewusst unveraendert -
/// der steigende Zaehler bei altem LastSeen zeigt die Regression.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/telemetry")]
public class SetErrorGroupStatusHandler(
    AppDbContext dbContext
) : IRequestHandler<SetErrorGroupStatusRequest, SetErrorGroupStatusResponse>
{
    // Id bewusst im Body statt als Route-Parameter: der Shiny-Mediator-Generator
    // bindet POST-Requests nur aus dem Body, Route-Parameter blieben leer (Guid.Empty)
    [MediatorHttpPost("/error-groups/status", OperationId = "SetTelemetryErrorGroupStatus", RequiresAuthorization = true, AuthorizationPolicies = [AuthorizationPolicies.RequireAdmin])]
    public async Task<SetErrorGroupStatusResponse> Handle(SetErrorGroupStatusRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        var group = await dbContext.Set<TelemetryErrorGroup>()
            .FirstOrDefaultAsync(g => g.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Fehlergruppe {request.Id} nicht gefunden");

        group.Status = request.Status;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new SetErrorGroupStatusResponse(group.Id, group.Status);
    }
}
