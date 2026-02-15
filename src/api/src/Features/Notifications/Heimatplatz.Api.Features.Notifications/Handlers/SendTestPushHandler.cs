using Fitomad.Apns;
using Fitomad.Apns.Entities.Notification;
using FirebaseAdmin.Messaging;
using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Notifications.Configuration;
using Heimatplatz.Api.Features.Notifications.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Notifications.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Notifications.Handlers;

/// <summary>
/// Handler to send test push notifications to all registered devices (Debug only)
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/notifications")]
public class SendTestPushHandler(
    AppDbContext dbContext,
    ILogger<SendTestPushHandler> logger,
    IOptions<PushNotificationOptions> options,
    IApnsClient? apnsClient = null
) : IRequestHandler<SendTestPushRequest, SendTestPushResponse>
{
    private static readonly string[] ApplePlatforms = ["iOS", "MacCatalyst", "maccatalyst"];

    [MediatorHttpPost("/test-push", OperationId = "SendTestPush")]
    public async Task<SendTestPushResponse> Handle(
        SendTestPushRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var allSubscriptions = await dbContext.Set<PushSubscription>()
            .ToListAsync(cancellationToken);

        if (allSubscriptions.Count == 0)
        {
            return new SendTestPushResponse(0, "No devices registered for push notifications");
        }

        var androidSubs = allSubscriptions.Where(s => s.Platform == "Android").ToList();
        var appleSubs = allSubscriptions.Where(s => ApplePlatforms.Contains(s.Platform, StringComparer.OrdinalIgnoreCase)).ToList();

        int totalSent = 0;
        var messages = new List<string>();

        // Send to Android via Firebase
        if (androidSubs.Count > 0)
        {
            if (FirebaseMessaging.DefaultInstance == null)
            {
                messages.Add($"Firebase not configured, skipping {androidSubs.Count} Android devices");
            }
            else
            {
                var tokens = androidSubs.Select(s => s.DeviceToken).ToList();
                var message = new MulticastMessage
                {
                    Tokens = tokens,
                    Notification = new FirebaseAdmin.Messaging.Notification
                    {
                        Title = request.Title,
                        Body = request.Body
                    },
                    Data = new Dictionary<string, string> { ["action"] = "test" },
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

                totalSent += response.SuccessCount;
                messages.Add($"Android: {response.SuccessCount}/{androidSubs.Count}");

                logger.LogInformation("Test push Android: {Success}/{Total}", response.SuccessCount, androidSubs.Count);
            }
        }

        // Send to iOS via APNs
        if (appleSubs.Count > 0)
        {
            if (apnsClient == null || !options.Value.Apns.Enabled)
            {
                messages.Add($"APNs not configured, skipping {appleSubs.Count} Apple devices");
            }
            else
            {
                int apnsSent = 0;
                foreach (var sub in appleSubs)
                {
                    try
                    {
                        var alert = new Alert
                        {
                            Title = request.Title,
                            Body = request.Body
                        };

                        var notification = new NotificationBuilder()
                            .WithAlert(alert)
                            .WithCategory("test")
                            .Build();

                        var response = await apnsClient.SendAsync(notification, deviceToken: sub.DeviceToken);

                        if (response.IsSuccess)
                        {
                            apnsSent++;
                            logger.LogInformation("APNs test push SUCCESS for {Token}",
                                sub.DeviceToken[..Math.Min(20, sub.DeviceToken.Length)] + "...");
                        }
                        else
                        {
                            var errorDetail = response.Error != null
                                ? $"Reason={response.Error.Reason}, Timestamp={response.Error.TimestampInMs}"
                                : "No error details";
                            logger.LogWarning("APNs test push FAILED for {Token}: {Error}, StatusCode={StatusCode}",
                                sub.DeviceToken[..Math.Min(20, sub.DeviceToken.Length)] + "...",
                                errorDetail,
                                response.StatusCode);
                            messages.Add($"APNs error: {errorDetail}");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error sending APNs test push to {Token}",
                            sub.DeviceToken[..Math.Min(20, sub.DeviceToken.Length)] + "...");
                        messages.Add($"APNs exception: {ex.Message}");
                    }
                }

                totalSent += apnsSent;
                messages.Add($"iOS/APNs: {apnsSent}/{appleSubs.Count}");

                logger.LogInformation("Test push APNs: {Success}/{Total}", apnsSent, appleSubs.Count);
            }
        }

        var summary = string.Join(", ", messages);
        return new SendTestPushResponse(totalSent, $"Sent {totalSent}/{allSubscriptions.Count}: {summary}");
    }
}
