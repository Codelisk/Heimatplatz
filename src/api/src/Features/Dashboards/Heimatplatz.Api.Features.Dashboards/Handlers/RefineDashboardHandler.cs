using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Dashboards.Configuration;
using Heimatplatz.Api.Features.Dashboards.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Dashboards.Contracts.Models;
using Heimatplatz.Api.Features.Dashboards.Data.Entities;
using Heimatplatz.Api.Features.Dashboards.Infrastructure;
using Heimatplatz.Api.Features.Dashboards.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Dashboards.Handlers;

/// <summary>
/// Startet eine Verfeinerungsrunde: neue Revision mit der Anweisung, Status Queued,
/// KI-Job. Die bestehende Definition bleibt bis zum Abschluss sichtbar (das UI kann
/// "wird überarbeitet" ueber der alten Fassung zeigen) und als Revision erhalten.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/dashboards")]
public class RefineDashboardHandler(
    AppDbContext dbContext,
    IDashboardGenerationJobScheduler jobScheduler,
    IHttpContextAccessor httpContextAccessor,
    IOptions<DashboardOptions> options
) : IRequestHandler<RefineDashboardRequest, RefineDashboardResponse>
{
    [MediatorHttpPost("/refine", OperationId = "RefineDashboard", RequiresAuthorization = true)]
    public async Task<RefineDashboardResponse> Handle(
        RefineDashboardRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var userId = httpContextAccessor.GetRequiredUserId();
        var limits = options.Value.Limits;

        var instruction = request.Instruction.Trim();
        if (instruction.Length < GenerateDashboardHandler.MinPromptLength)
            throw new ArgumentException("Bitte beschreiben Sie kurz, was an der Übersicht geändert werden soll.");
        if (instruction.Length > limits.MaxPromptChars)
            throw new ArgumentException($"Der Wunsch ist zu lang (maximal {limits.MaxPromptChars} Zeichen).");

        var dashboard = await dbContext.Set<UserDashboard>()
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Übersicht mit ID {request.Id} nicht gefunden.");

        if (dashboard.UserId != userId)
            throw new UnauthorizedAccessException("Sie haben keine Berechtigung für diese Übersicht.");

        if (dashboard.GenerationStatus is DashboardGenerationStatus.Queued or DashboardGenerationStatus.InProgress)
            throw new ArgumentException("Diese Übersicht wird gerade erstellt - bitte warten Sie den Abschluss ab.");

        await GenerateDashboardHandler.EnsureDailyQuotaAsync(dbContext, userId, limits, cancellationToken);

        dashboard.GenerationStatus = DashboardGenerationStatus.Queued;
        dashboard.GenerationError = null;
        dashboard.GenerationRequestedAt = DateTimeOffset.UtcNow;
        dashboard.GenerationCompletedAt = null;

        var revision = new UserDashboardRevision
        {
            Id = Guid.NewGuid(),
            DashboardId = dashboard.Id,
            UserPrompt = instruction
        };
        dbContext.Set<UserDashboardRevision>().Add(revision);
        await dbContext.SaveChangesAsync(cancellationToken);

        await jobScheduler.ScheduleAsync(revision.Id, cancellationToken);

        return new RefineDashboardResponse(dashboard.GenerationStatus);
    }
}
