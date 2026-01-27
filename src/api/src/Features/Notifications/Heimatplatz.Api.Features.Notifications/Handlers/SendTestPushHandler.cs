using FirebaseAdmin.Messaging;
using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Notifications.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Notifications.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Notifications.Handlers;

/// <summary>
/// Handler to send test push notifications (Debug only)
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/notifications")]
public class SendTestPushHandler(
    AppDbContext dbContext,
    ILogger<SendTestPushHandler> logger
) : IRequestHandler<SendTestPushRequest, SendTestPushResponse>
{
    [MediatorHttpPost("/test-push", OperationId = "SendTestPush")]
    public async Task<SendTestPushResponse> Handle(
        SendTestPushRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        // Get all registered Android devices
        var subscriptions = await dbContext.Set<PushSubscription>()
            .Where(ps => ps.Platform == "Android")
            .ToListAsync(cancellationToken);

        if (subscriptions.Count == 0)
        {
            return new SendTestPushResponse(0, "No Android devices registered for push notifications");
        }

        if (FirebaseMessaging.DefaultInstance == null)
        {
            return new SendTestPushResponse(0, "Firebase is not configured");
        }

        var tokens = subscriptions.Select(s => s.DeviceToken).ToList();

        var message = new MulticastMessage
        {
            Tokens = tokens,
            Notification = new Notification
            {
                Title = request.Title,
                Body = request.Body
            },
            Data = new Dictionary<string, string>
            {
                ["action"] = "test"
            },
            Android = new AndroidConfig
            {
                Notification = new AndroidNotification
                {
                    ClickAction = "SHINY_PUSH_NOTIFICATION_CLICK"
                }
            }
        };

        var response = await FirebaseMessaging.DefaultInstance
            .SendEachForMulticastAsync(message, cancellationToken);

        logger.LogInformation(
            "Test push sent: {Success}/{Total} successful",
            response.SuccessCount,
            subscriptions.Count);

        return new SendTestPushResponse(
            response.SuccessCount,
            $"Sent to {response.SuccessCount}/{subscriptions.Count} devices");
    }
}
