using System.Text.Json;
using System.Text.Json.Serialization;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.AiListing.Contracts.Enums;
using Heimatplatz.Api.Features.AiListing.Data.Entities;
using Heimatplatz.Api.Features.AiListing.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Heimatplatz.Api.Features.AiListing.Infrastructure;

/// <summary>
/// Hintergrund-Worker: arbeitet KI-Analyse-Jobs aus der Queue ab
/// (Queued → InProgress → Finished/Failed).
/// Beim Start werden liegengebliebene Jobs (z.B. nach einem Neustart) erneut eingereiht.
/// </summary>
public class ListingAnalysisWorker(
    ListingAnalysisQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<ListingAnalysisWorker> logger
) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RequeuePendingJobsAsync(stoppingToken);

        await foreach (var analysisId in queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                await ProcessAsync(analysisId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[AiListing] Unerwarteter Fehler bei Analyse {AnalysisId}", analysisId);
            }
        }
    }

    private async Task RequeuePendingJobsAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var pendingIds = await dbContext.Set<ListingAnalysis>()
                .Where(a => a.Status == ListingAnalysisStatus.Queued || a.Status == ListingAnalysisStatus.InProgress)
                .Select(a => a.Id)
                .ToListAsync(ct);

            foreach (var id in pendingIds)
                await queue.EnqueueAsync(id, ct);

            if (pendingIds.Count > 0)
                logger.LogInformation("[AiListing] {Count} liegengebliebene Analyse-Jobs erneut eingereiht", pendingIds.Count);
        }
        catch (Exception ex)
        {
            // Z.B. beim OpenAPI-Build ohne Datenbank: Worker darf den Host nicht crashen
            logger.LogWarning(ex, "[AiListing] Requeue beim Start uebersprungen (Datenbank nicht verfuegbar?)");
        }
    }

    private async Task ProcessAsync(Guid analysisId, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var mediaService = scope.ServiceProvider.GetRequiredService<IListingMediaService>();
        var extractionService = scope.ServiceProvider.GetRequiredService<IListingExtractionService>();

        var analysis = await dbContext.Set<ListingAnalysis>()
            .FirstOrDefaultAsync(a => a.Id == analysisId, ct);

        if (analysis is null)
        {
            logger.LogWarning("[AiListing] Analyse {AnalysisId} nicht gefunden", analysisId);
            return;
        }

        if (analysis.Status is ListingAnalysisStatus.Finished or ListingAnalysisStatus.Failed)
            return;

        logger.LogInformation("[AiListing] Analyse {AnalysisId} gestartet ({Images} Fotos, {Videos} Videos)",
            analysisId, analysis.ImageUrls.Count, analysis.VideoUrls.Count);

        analysis.Status = ListingAnalysisStatus.InProgress;
        analysis.StartedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(ct);

        try
        {
            var imagePaths = analysis.ImageUrls
                .Select(mediaService.ResolvePhysicalPath)
                .Where(p => p is not null)
                .Select(p => p!)
                .ToList();

            var videoPaths = analysis.VideoUrls
                .Select(mediaService.ResolvePhysicalPath)
                .Where(p => p is not null)
                .Select(p => p!)
                .ToList();

            var input = new ListingExtractionInput(imagePaths, videoPaths, analysis.DictatedText, analysis.UserNotes);
            var result = await extractionService.ExtractAsync(input, ct);

            analysis.Status = ListingAnalysisStatus.Finished;
            analysis.ResultJson = JsonSerializer.Serialize(result, JsonOptions);
            analysis.CompletedAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(ct);

            logger.LogInformation("[AiListing] Analyse {AnalysisId} abgeschlossen: {Title}", analysisId, result.Title);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[AiListing] Analyse {AnalysisId} fehlgeschlagen", analysisId);

            analysis.Status = ListingAnalysisStatus.Failed;
            analysis.ErrorMessage = ex.Message;
            analysis.CompletedAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }
    }
}
