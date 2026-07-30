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
/// Fuehrt eine Schnellaktion der Akquise-Arbeitsliste aus (siehe
/// <see cref="QuickMarketingContactActionRequest"/> fuer die Semantik je Aktion).
/// Der Grund landet als Notiz direkt am StatusChange-Eintrag, damit die Timeline
/// "Status: Kontaktiert -&gt; Kein Interesse" samt Begruendung in einem Eintrag zeigt.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin/marketing")]
public class QuickMarketingContactActionHandler(
    AppDbContext dbContext,
    IAdminAccessGuard accessGuard
) : IRequestHandler<QuickMarketingContactActionRequest, QuickMarketingContactActionResponse>
{
    private const int MaxReasonLength = 500;

    [MediatorHttpPost("/contacts/quick", OperationId = "QuickMarketingContactAction")]
    public async Task<QuickMarketingContactActionResponse> Handle(QuickMarketingContactActionRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        accessGuard.EnsureAuthorized();

        var contact = await dbContext.Set<MarketingContact>()
            .FirstOrDefaultAsync(x => x.Id == request.ContactId, cancellationToken);

        if (contact is null)
            return Fail("Kontakt nicht gefunden.");

        var occurredAt = DateTimeOffset.UtcNow;
        var previousStatus = contact.Status;
        var previousFollowUp = contact.NextFollowUpAt;

        var reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
        if (reason is { Length: > MaxReasonLength })
            reason = reason[..MaxReasonLength];

        switch (request.Action)
        {
            case MarketingQuickAction.Interested:
                ChangeStatus(contact, MarketingContactStatus.Interested, reason, occurredAt);
                break;

            case MarketingQuickAction.Reject:
                ChangeStatus(contact, MarketingContactStatus.NotInterested, reason, occurredAt);
                break;

            case MarketingQuickAction.Block:
                ChangeStatus(contact, MarketingContactStatus.DoNotContact, reason, occurredAt);
                break;

            case MarketingQuickAction.Snooze:
                if (request.FollowUpAt is null)
                    return Fail("Wiedervorlage-Termin fehlt.");

                contact.NextFollowUpAt = request.FollowUpAt;
                dbContext.Set<MarketingActivity>().Add(new MarketingActivity
                {
                    ContactId = contact.Id,
                    Type = MarketingActivityType.FollowUp,
                    Notes = reason,
                    FollowUpAt = request.FollowUpAt,
                    OccurredAt = occurredAt
                });

                // Arbeitsstatus wandert mit in "Wiedervorlage", damit die Zu-kontaktieren-
                // Liste sauber bleibt; Interessiert/Kunde & Co. bleiben unveraendert
                if (contact.Status is MarketingContactStatus.ToContact
                    or MarketingContactStatus.Contacted
                    or MarketingContactStatus.Replied)
                    ChangeStatus(contact, MarketingContactStatus.FollowUp, null, occurredAt);
                break;

            case MarketingQuickAction.NotReached:
                // Kontaktversuch ohne Gespraech: Status und LastContactedAt bleiben
                // unveraendert - erreicht wurde niemand
                dbContext.Set<MarketingActivity>().Add(new MarketingActivity
                {
                    ContactId = contact.Id,
                    Type = MarketingActivityType.Call,
                    Notes = reason,
                    FollowUpAt = request.FollowUpAt,
                    OccurredAt = occurredAt
                });

                if (request.FollowUpAt is not null)
                    contact.NextFollowUpAt = request.FollowUpAt;
                break;

            case MarketingQuickAction.Restore:
                if (request.RestoreStatus is not { } restoreStatus)
                    return Fail("Zielstatus fehlt.");

                contact.NextFollowUpAt = request.RestoreFollowUpAt;
                if (contact.Status != restoreStatus)
                {
                    dbContext.Set<MarketingActivity>().Add(
                        MarketingActivity.StatusChange(contact.Id, contact.Status, restoreStatus, occurredAt));
                    contact.Status = restoreStatus;
                }
                break;

            default:
                return Fail("Unbekannte Aktion.");
        }

        contact.UpdatedAt = occurredAt;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new QuickMarketingContactActionResponse(
            true, null, contact.Status, contact.NextFollowUpAt, previousStatus, previousFollowUp);
    }

    /// <summary>
    /// Statuswechsel inkl. Timeline-Eintrag; bei unveraendertem Status wird ein gesetzter
    /// Grund trotzdem als Notiz festgehalten (z.B. zweite Ablehnung mit anderem Grund).
    /// </summary>
    private void ChangeStatus(MarketingContact contact, MarketingContactStatus to, string? reason, DateTimeOffset occurredAt)
    {
        if (contact.Status == to)
        {
            if (reason is not null)
                dbContext.Set<MarketingActivity>().Add(new MarketingActivity
                {
                    ContactId = contact.Id,
                    Type = MarketingActivityType.Note,
                    Notes = reason,
                    OccurredAt = occurredAt
                });
            return;
        }

        dbContext.Set<MarketingActivity>().Add(
            MarketingActivity.StatusChange(contact.Id, contact.Status, to, occurredAt, reason));
        contact.Status = to;

        // Endstatus -> offene Wiedervorlage ist damit erledigt (gleiche Regel wie
        // LogMarketingActivityHandler)
        if (to.IsClosed())
            contact.NextFollowUpAt = null;
    }

    private static QuickMarketingContactActionResponse Fail(string error) =>
        new(false, error, null, null, null, null);
}
