using Heimatplatz.Api;
using Heimatplatz.Api.Features.Notifications.Contracts.Events;
using Heimatplatz.Api.Features.Notifications.Services;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Notifications.Handlers;

/// <summary>
/// Schickt bei einer Team-Antwort auf eine Feedback-Anfrage die Push-Benachrichtigung
/// an alle Geraete des Anfrage-Besitzers.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class FeedbackTeamRepliedEventHandler(
    IPushNotificationService pushNotificationService,
    ILogger<FeedbackTeamRepliedEventHandler> logger
) : IEventHandler<FeedbackTeamRepliedEvent>
{
    public async Task Handle(FeedbackTeamRepliedEvent @event, IMediatorContext context, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Processing FeedbackTeamRepliedEvent for ticket {TicketId} (user {UserId})",
            @event.TicketId,
            @event.UserId);

        await pushNotificationService.SendFeedbackReplyNotificationAsync(
            @event.TicketId,
            @event.UserId,
            @event.Subject,
            @event.BodyPreview,
            cancellationToken);
    }
}
