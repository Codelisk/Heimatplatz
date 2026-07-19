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
/// Alle persistierten Spans und Logs eines Traces, zeitlich sortiert -
/// die Datenbasis fuer eine Waterfall-Darstellung.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/telemetry")]
public class GetTraceDetailHandler(
    AppDbContext dbContext
) : IRequestHandler<GetTraceDetailRequest, GetTraceDetailResponse>
{
    [MediatorHttpGet("/traces/{TraceId}", OperationId = "GetTelemetryTraceDetail", RequiresAuthorization = true, AuthorizationPolicies = [AuthorizationPolicies.RequireAdmin])]
    public async Task<GetTraceDetailResponse> Handle(GetTraceDetailRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        var traceId = request.TraceId.ToLowerInvariant();
        if (traceId.Length != 32 || !traceId.All(char.IsAsciiHexDigitLower))
        {
            throw new ValidationException("TraceId muss aus 32 Hex-Zeichen bestehen");
        }

        var spans = await dbContext.Set<TelemetrySpan>()
            .AsNoTracking()
            .Where(s => s.TraceId == traceId)
            .ToListAsync(cancellationToken);

        var logs = await dbContext.Set<TelemetryLog>()
            .AsNoTracking()
            .Where(l => l.TraceId == traceId)
            .ToListAsync(cancellationToken);

        // Sortierung in-memory: kleine Mengen, unabhaengig vom Provider
        return new GetTraceDetailResponse(
            traceId,
            spans.OrderBy(s => s.StartTimeUtc).Select(TelemetryQueryHelpers.Map).ToList(),
            logs.OrderBy(l => l.TimestampUtc).Select(TelemetryQueryHelpers.Map).ToList());
    }
}
