using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.AiListing.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.PropertyDrafts.Contracts.Models;
using Heimatplatz.Api.Features.PropertyDrafts.Data.Entities;
using Heimatplatz.Api.Features.PropertyDrafts.Infrastructure;
using Heimatplatz.Api.Features.PropertyDrafts.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.PropertyDrafts.Services;

/// <summary>
/// Fachlicher Kern des Beschreibungs-Hintergrund-Jobs: laedt den Entwurf, laesst die
/// Beschreibung ueber das AiListing-Feature generieren (in-process Mediator-Request,
/// Provider Mock/AiConnector) und schreibt Ergebnis bzw. Fehler in die Description*-Spalten.
/// Von <see cref="Jobs.DraftDescriptionJob"/> pro Ausfuehrung in einem eigenen DI-Scope
/// aufgerufen; Integrationstests rufen ihn direkt auf.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class DraftDescriptionProcessor(
    AppDbContext dbContext,
    IMediator mediator,
    ILogger<DraftDescriptionProcessor> logger
)
{
    const int MaxErrorLength = 2000;

    /// <summary>
    /// Ein Ausfuehrungs-Versuch. Wirft bei Fehlern, solange noch Retries offen sind
    /// (TickerQ plant dann den naechsten Versuch); beim letzten Versuch wird stattdessen
    /// Status Failed am Entwurf persistiert.
    /// </summary>
    public async Task ProcessAsync(Guid draftId, int retryCount, CancellationToken cancellationToken)
    {
        var draft = await dbContext.Set<PropertyDraft>()
            .FirstOrDefaultAsync(d => d.Id == draftId, cancellationToken);

        if (draft is null)
        {
            // Entwurf wurde zwischenzeitlich geloescht oder veroeffentlicht - nichts zu tun
            logger.LogInformation("[PropertyDrafts] Beschreibungs-Job: Entwurf {DraftId} existiert nicht mehr", draftId);
            return;
        }

        if (draft.DescriptionStatus == DraftDescriptionStatus.Finished)
            return;

        try
        {
            draft.DescriptionStatus = DraftDescriptionStatus.InProgress;
            await dbContext.SaveChangesAsync(cancellationToken);

            var data = DraftPayloadSerializer.Deserialize(draft.PayloadJson);
            var request = new GenerateListingDescriptionRequest
            {
                Type = data.Type,
                Title = data.Title,
                Rooms = data.Rooms,
                LivingAreaSquareMeters = data.LivingAreaSquareMeters,
                PlotAreaSquareMeters = data.PlotAreaSquareMeters,
                YearBuilt = data.YearBuilt,
                Features = data.Features,
                MunicipalityDisplay = data.MunicipalityDisplay,
                Keywords = draft.DescriptionKeywords ?? "",
                ImageUrls = data.ImageUrls
            };

            logger.LogInformation("[PropertyDrafts] Beschreibungs-Job fuer Entwurf {DraftId} gestartet (Versuch {Attempt})",
                draftId, retryCount + 1);

            var result = await mediator.Request(request, cancellationToken);

            draft.GeneratedDescription = result.Result.Description;
            draft.DescriptionStatus = DraftDescriptionStatus.Finished;
            draft.DescriptionError = null;
            draft.DescriptionCompletedAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("[PropertyDrafts] Beschreibungs-Job fuer Entwurf {DraftId} abgeschlossen", draftId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (retryCount >= DraftDescriptionJob.MaxRetries)
        {
            // Letzter Versuch: Fehler persistieren statt weiter zu werfen. ExecuteUpdate
            // umgeht den nach der Exception potenziell verschmutzten ChangeTracker.
            logger.LogError(ex, "[PropertyDrafts] Beschreibungs-Job fuer Entwurf {DraftId} endgueltig fehlgeschlagen", draftId);

            var message = ex.Message.Length > MaxErrorLength ? ex.Message[..MaxErrorLength] : ex.Message;
            await dbContext.Set<PropertyDraft>()
                .Where(d => d.Id == draftId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(d => d.DescriptionStatus, DraftDescriptionStatus.Failed)
                    .SetProperty(d => d.DescriptionError, message)
                    .SetProperty(d => d.DescriptionCompletedAt, DateTimeOffset.UtcNow),
                    CancellationToken.None);
        }
    }
}
