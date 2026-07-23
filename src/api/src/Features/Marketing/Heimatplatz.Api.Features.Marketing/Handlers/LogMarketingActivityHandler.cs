using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Admin.Services;
using Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Marketing.Contracts.Models;
using Heimatplatz.Api.Features.Marketing.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Handlers;

/// <summary>
/// Haelt einen Bearbeitungsschritt am Kontakt fest (Anruf, Notiz, Termin) und zieht die
/// Folgen am Kontakt nach: Status, Wiedervorlage, letzter Kontakt.
/// Ein Statuswechsel wird als eigener Historien-Eintrag protokolliert, damit in der
/// Timeline nachvollziehbar bleibt, wann sich der Funnel-Status geaendert hat.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin/marketing")]
public class LogMarketingActivityHandler(
    AppDbContext dbContext,
    IAdminAccessGuard accessGuard
) : IRequestHandler<LogMarketingActivityRequest, LogMarketingActivityResponse>
{
    private const int MaxNotesLength = 4000;

    [MediatorHttpPost("/contacts/activity", OperationId = "LogMarketingActivity")]
    public async Task<LogMarketingActivityResponse> Handle(LogMarketingActivityRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        accessGuard.EnsureAuthorized();

        var contact = await dbContext.Set<MarketingContact>()
            .FirstOrDefaultAsync(x => x.Id == request.ContactId, cancellationToken);

        if (contact is null)
            return new LogMarketingActivityResponse(false, null, "Kontakt nicht gefunden.");

        var notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        if (notes is { Length: > MaxNotesLength })
            notes = notes[..MaxNotesLength];

        // Eine Notiz ohne Text und ohne Wirkung waere ein leerer Timeline-Eintrag
        if (notes is null && request.FollowUpAt is null && request.NewStatus is null)
            return new LogMarketingActivityResponse(false, null, "Bitte eine Notiz, einen Termin oder einen Status angeben.");

        var occurredAt = request.OccurredAt ?? DateTimeOffset.UtcNow;

        var activity = new MarketingActivity
        {
            ContactId = contact.Id,
            Type = request.Type,
            Notes = notes,
            FollowUpAt = request.FollowUpAt,
            OccurredAt = occurredAt
        };
        dbContext.Set<MarketingActivity>().Add(activity);

        // Telefonat/Termin ist eine echte Kontaktaufnahme - auch ohne Mailversand
        if (request.Type is MarketingActivityType.Call or MarketingActivityType.Meeting)
            contact.LastContactedAt = occurredAt;

        if (request.FollowUpAt is not null)
            contact.NextFollowUpAt = request.FollowUpAt;

        if (request.NewStatus is { } newStatus && newStatus != contact.Status)
        {
            dbContext.Set<MarketingActivity>().Add(new MarketingActivity
            {
                ContactId = contact.Id,
                Type = MarketingActivityType.StatusChange,
                StatusFrom = contact.Status,
                StatusTo = newStatus,
                OccurredAt = occurredAt
            });

            contact.Status = newStatus;
        }

        contact.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new LogMarketingActivityResponse(true, activity.Id, null);
    }
}
