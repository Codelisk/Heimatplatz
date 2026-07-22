using Heimatplatz.Api.Cleanup;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Feedback.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Heimatplatz.Api.Features.Feedback.Services;

/// <summary>
/// Loescht bei der Konto-Loeschung alle Feedback-Anfragen des Benutzers inklusive
/// Verlauf und Anhang-Dateien auf der Platte.
/// Registrierung erfolgt in <c>AddFeedbackFeature</c>.
/// </summary>
public class FeedbackUserDataEraser(
    AppDbContext dbContext,
    IFeedbackAttachmentService attachmentService
) : IUserDataEraser
{
    /// <summary>Keine FK-Abhaengigkeiten zu anderen Features -> frueh ausfuehrbar.</summary>
    public int Order => 15;

    public async Task EraseUserDataAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var ticketIds = await dbContext.Set<FeedbackTicket>()
            .Where(t => t.UserId == userId)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        if (ticketIds.Count == 0)
            return;

        var attachmentUrls = await dbContext.Set<FeedbackAttachment>()
            .Where(a => ticketIds.Contains(a.Message.TicketId))
            .Select(a => a.Url)
            .ToListAsync(cancellationToken);

        await attachmentService.DeleteAttachmentFilesAsync(attachmentUrls, cancellationToken);

        // Explizit in FK-sicherer Reihenfolge statt auf DB-Kaskaden zu vertrauen
        await dbContext.Set<FeedbackAttachment>()
            .Where(a => ticketIds.Contains(a.Message.TicketId))
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.Set<FeedbackMessage>()
            .Where(m => ticketIds.Contains(m.TicketId))
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.Set<FeedbackTicket>()
            .Where(t => t.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
