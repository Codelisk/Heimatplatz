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
/// Detail einer Fehlergruppe inkl. Sample-Stacktrace und der letzten Auftreten
/// (Einstieg: von dort per TraceId in die Waterfall-Ansicht).
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/telemetry")]
public class GetErrorGroupDetailHandler(
    AppDbContext dbContext
) : IRequestHandler<GetErrorGroupDetailRequest, GetErrorGroupDetailResponse>
{
    [MediatorHttpGet("/error-groups/{Id}", OperationId = "GetTelemetryErrorGroupDetail", RequiresAuthorization = true, AuthorizationPolicies = [AuthorizationPolicies.RequireAdmin])]
    public async Task<GetErrorGroupDetailResponse> Handle(GetErrorGroupDetailRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        var group = await dbContext.Set<TelemetryErrorGroup>()
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Fehlergruppe {request.Id} nicht gefunden");

        var limit = Math.Clamp(request.OccurrenceLimit, 1, 100);

        var occurrences = await dbContext.Set<TelemetryLog>()
            .AsNoTracking()
            .Where(l => l.ErrorGroupId == request.Id)
            .OrderByDescending(l => l.TimestampUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return new GetErrorGroupDetailResponse(
            TelemetryQueryHelpers.Map(group),
            group.SampleStackTrace,
            occurrences.Select(TelemetryQueryHelpers.Map).ToList());
    }
}
