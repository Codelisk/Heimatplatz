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
/// Erstellt eine neue Uebersicht aus dem Freitext-Wunsch: legt das Dashboard mit
/// Status Queued + die erste Revision an und plant den KI-Hintergrund-Job ein.
/// Der Client pollt anschliessend GetDashboard. Quoten (Anzahl Uebersichten,
/// Generierungen pro Tag) werden hier fail-closed durchgesetzt - sie schuetzen
/// die AiConnector-Kosten.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/dashboards")]
public class GenerateDashboardHandler(
    AppDbContext dbContext,
    IDashboardGenerationJobScheduler jobScheduler,
    IHttpContextAccessor httpContextAccessor,
    IOptions<DashboardOptions> options
) : IRequestHandler<GenerateDashboardRequest, GenerateDashboardResponse>
{
    public const int MinPromptLength = 5;

    [MediatorHttpPost("/generate", OperationId = "GenerateDashboard", RequiresAuthorization = true)]
    public async Task<GenerateDashboardResponse> Handle(
        GenerateDashboardRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var userId = httpContextAccessor.GetRequiredUserId();
        var limits = options.Value.Limits;

        var prompt = request.Prompt.Trim();
        if (prompt.Length < MinPromptLength)
            throw new ArgumentException("Bitte beschreiben Sie kurz, wonach Sie suchen und was Sie sehen möchten.");
        if (prompt.Length > limits.MaxPromptChars)
            throw new ArgumentException($"Der Wunsch ist zu lang (maximal {limits.MaxPromptChars} Zeichen).");

        var dashboardCount = await dbContext.Set<UserDashboard>()
            .CountAsync(d => d.UserId == userId, cancellationToken);
        if (dashboardCount >= limits.MaxPerUser)
            throw new ArgumentException(
                $"Sie haben bereits {dashboardCount} Übersichten. Löschen Sie zuerst eine, um eine neue zu erstellen.");

        await EnsureDailyQuotaAsync(dbContext, userId, limits, cancellationToken);

        var dashboard = new UserDashboard
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = "Neue Übersicht",
            GenerationStatus = DashboardGenerationStatus.Queued,
            GenerationRequestedAt = DateTimeOffset.UtcNow
        };
        var revision = new UserDashboardRevision
        {
            Id = Guid.NewGuid(),
            DashboardId = dashboard.Id,
            UserPrompt = prompt
        };

        dbContext.Set<UserDashboard>().Add(dashboard);
        dbContext.Set<UserDashboardRevision>().Add(revision);
        await dbContext.SaveChangesAsync(cancellationToken);

        await jobScheduler.ScheduleAsync(revision.Id, cancellationToken);

        return new GenerateDashboardResponse(dashboard.Id, dashboard.GenerationStatus);
    }

    /// <summary>
    /// Rollierendes 24h-Fenster ueber alle Revisionen (Erstellen + Verfeinern) des
    /// Nutzers. Auch vom RefineDashboardHandler verwendet.
    /// </summary>
    internal static async Task EnsureDailyQuotaAsync(
        AppDbContext dbContext,
        Guid userId,
        DashboardLimitOptions limits,
        CancellationToken cancellationToken)
    {
        var since = DateTimeOffset.UtcNow.AddDays(-1);
        var userDashboardIds = dbContext.Set<UserDashboard>()
            .Where(d => d.UserId == userId)
            .Select(d => d.Id);

        var generationsToday = await dbContext.Set<UserDashboardRevision>()
            .CountAsync(r => userDashboardIds.Contains(r.DashboardId) && r.CreatedAt >= since, cancellationToken);

        if (generationsToday >= limits.MaxGenerationsPerDay)
            throw new ArgumentException(
                "Das Tageslimit für KI-Generierungen ist erreicht. Bitte versuchen Sie es morgen erneut.");
    }
}
