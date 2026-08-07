using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Dashboards.Contracts.Models;
using Heimatplatz.Api.Features.Dashboards.Data.Entities;
using Heimatplatz.Api.Features.Dashboards.Infrastructure;
using Heimatplatz.Api.Features.Dashboards.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shiny;

namespace Heimatplatz.Api.Features.Dashboards.Services;

/// <summary>
/// Fachlicher Kern des Generierungs-Jobs: laedt die Revision, laesst den Designer
/// (Provider Mock/AiConnector) die Definition entwerfen und schickt das Ergebnis
/// durch Parser + fail-closed-Validator, bevor es am Dashboard landet.
/// Von <see cref="Jobs.DashboardGenerationJob"/> pro Ausfuehrung in einem eigenen
/// DI-Scope aufgerufen; Integrationstests rufen ihn direkt auf.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class DashboardGenerationProcessor(
    AppDbContext dbContext,
    IDashboardDesigner designer,
    DashboardDefinitionValidator validator,
    ILogger<DashboardGenerationProcessor> logger
)
{
    const int MaxRawExcerptLength = 8000;

    public const string GenericErrorMessage =
        "Ihre Übersicht konnte gerade nicht erstellt werden. Bitte versuchen Sie es später erneut.";

    public const string TimeoutErrorMessage =
        "Die Erstellung hat zu lange gedauert. Bitte versuchen Sie es erneut.";

    /// <summary>
    /// Ein Ausfuehrungs-Versuch. Wirft bei Fehlern, solange noch Retries offen sind
    /// (TickerQ plant dann den naechsten Versuch); beim letzten Versuch wird stattdessen
    /// Status Failed mit nutzerfreundlicher Meldung am Dashboard persistiert.
    /// </summary>
    public async Task ProcessAsync(Guid revisionId, int retryCount, CancellationToken cancellationToken)
    {
        var revision = await dbContext.Set<UserDashboardRevision>()
            .FirstOrDefaultAsync(r => r.Id == revisionId, cancellationToken);

        if (revision is null)
        {
            // Dashboard (samt Revisionen, FK-Kaskade) wurde zwischenzeitlich geloescht
            logger.LogInformation("[Dashboards] Generierungs-Job: Revision {RevisionId} existiert nicht mehr", revisionId);
            return;
        }

        var dashboard = await dbContext.Set<UserDashboard>()
            .FirstOrDefaultAsync(d => d.Id == revision.DashboardId, cancellationToken);

        if (dashboard is null)
        {
            logger.LogInformation("[Dashboards] Generierungs-Job: Dashboard {DashboardId} existiert nicht mehr", revision.DashboardId);
            return;
        }

        // Idempotenz: diese Runde ist bereits fertig (z.B. doppelte Job-Ausfuehrung)
        if (revision.DefinitionJson is not null)
            return;

        string? rawOutput = null;
        try
        {
            dashboard.GenerationStatus = DashboardGenerationStatus.InProgress;
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("[Dashboards] Generierungs-Job fuer Dashboard {DashboardId} gestartet (Versuch {Attempt})",
                dashboard.Id, retryCount + 1);

            // Verfeinerung, wenn schon eine Definition existiert - sie ist die Basis
            rawOutput = await designer.DesignAsync(revision.UserPrompt, dashboard.ViewType, dashboard.DefinitionJson, cancellationToken);

            var parsed = DashboardOutputParser.Parse(rawOutput);
            var validated = await validator.ValidateAsync(parsed, dashboard.ViewType, cancellationToken);
            var definitionJson = DashboardDefinitionSerializer.Serialize(validated);

            dashboard.DefinitionJson = definitionJson;
            dashboard.SchemaVersion = validated.SchemaVersion;
            dashboard.Title = validated.Title;
            dashboard.GenerationStatus = DashboardGenerationStatus.Finished;
            dashboard.GenerationError = null;
            dashboard.GenerationCompletedAt = DateTimeOffset.UtcNow;

            revision.DefinitionJson = definitionJson;
            revision.RawOutputExcerpt = Truncate(rawOutput, MaxRawExcerptLength);

            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("[Dashboards] Generierungs-Job fuer Dashboard {DashboardId} abgeschlossen ({WidgetCount} Widgets)",
                dashboard.Id, validated.Widgets.Count);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (retryCount >= DashboardGenerationJob.MaxRetries)
        {
            // Letzter Versuch: Fehler persistieren statt weiter zu werfen. ExecuteUpdate
            // umgeht den nach der Exception potenziell verschmutzten ChangeTracker.
            logger.LogError(ex, "[Dashboards] Generierungs-Job fuer Dashboard {DashboardId} endgueltig fehlgeschlagen", dashboard.Id);

            var message = ex switch
            {
                DashboardValidationException validation => validation.Message,
                TimeoutException => TimeoutErrorMessage,
                _ => GenericErrorMessage
            };

            await dbContext.Set<UserDashboard>()
                .Where(d => d.Id == dashboard.Id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(d => d.GenerationStatus, DashboardGenerationStatus.Failed)
                    .SetProperty(d => d.GenerationError, message)
                    .SetProperty(d => d.GenerationCompletedAt, DateTimeOffset.UtcNow),
                    CancellationToken.None);

            // Rohausgabe fuers Prompt-Tuning aufheben (bestes Diagnose-Artefakt bei Parser-/Validator-Fehlern)
            if (rawOutput is not null)
            {
                var excerpt = Truncate(rawOutput, MaxRawExcerptLength);
                await dbContext.Set<UserDashboardRevision>()
                    .Where(r => r.Id == revisionId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(r => r.RawOutputExcerpt, excerpt),
                        CancellationToken.None);
            }
        }
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
