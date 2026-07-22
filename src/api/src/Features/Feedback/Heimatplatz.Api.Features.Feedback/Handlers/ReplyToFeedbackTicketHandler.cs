using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Admin.Services;
using Heimatplatz.Api.Features.Feedback.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Feedback.Contracts.Models;
using Heimatplatz.Api.Features.Feedback.Data.Entities;
using Heimatplatz.Api.Features.Feedback.Infrastructure;
using Heimatplatz.Api.Features.Notifications.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Feedback.Handlers;

/// <summary>
/// Intern: Team-Antwort auf eine Anfrage. Setzt den Status auf Answered, markiert die
/// Antwort fuer den Nutzer als ungelesen und loest ueber FeedbackTeamRepliedEvent die
/// Push-Benachrichtigung aus (Versand haengt im Notifications-Feature).
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin/feedback")]
public class ReplyToFeedbackTicketHandler(
    AppDbContext dbContext,
    IAdminAccessGuard accessGuard,
    IMediator mediator
) : IRequestHandler<ReplyToFeedbackTicketRequest, ReplyToFeedbackTicketResponse>
{
    [MediatorHttpPost("/reply", OperationId = "ReplyToFeedbackTicket")]
    public async Task<ReplyToFeedbackTicketResponse> Handle(
        ReplyToFeedbackTicketRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        accessGuard.EnsureAuthorized();

        var body = request.Body?.Trim();
        if (string.IsNullOrEmpty(body))
            return new ReplyToFeedbackTicketResponse(false, null, "Bitte gib eine Antwort ein.");

        if (body.Length > FeedbackValidation.MaxBodyLength)
            return new ReplyToFeedbackTicketResponse(false, null,
                $"Die Antwort ist zu lang (maximal {FeedbackValidation.MaxBodyLength} Zeichen).");

        var ticket = await dbContext.Set<FeedbackTicket>()
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

        if (ticket == null)
            return new ReplyToFeedbackTicketResponse(false, null, "Anfrage nicht gefunden.");

        var message = new FeedbackMessage
        {
            TicketId = ticket.Id,
            Author = FeedbackAuthor.Team,
            Body = body
        };

        dbContext.Set<FeedbackMessage>().Add(message);

        ticket.Status = FeedbackTicketStatus.Answered;
        ticket.HasUnreadForUser = true;
        ticket.LastMessageAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        await mediator.Publish(
            new FeedbackTeamRepliedEvent(
                ticket.Id,
                ticket.UserId,
                ticket.Subject,
                FeedbackMapping.BuildPreview(body, hasAttachments: false, maxLength: 140)),
            cancellationToken);

        return new ReplyToFeedbackTicketResponse(true, message.Id, null);
    }
}
