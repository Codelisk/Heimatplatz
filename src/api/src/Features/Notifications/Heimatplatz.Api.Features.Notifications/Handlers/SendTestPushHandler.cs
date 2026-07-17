using Heimatplatz.Api;
using Heimatplatz.Api.Authorization;
using Heimatplatz.Api.Features.Notifications.Contracts.Mediator.Requests;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Extensions.Push;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Notifications.Handlers;

/// <summary>
/// Handler to send a test push through all configured Shiny push providers.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/notifications")]
public class SendTestPushHandler(
    IPushManager pushManager,
    ILogger<SendTestPushHandler> logger
) : IRequestHandler<SendTestPushRequest, SendTestPushResponse>
{
    private const string ShinyAndroidClickAction = "SHINY_PUSH_NOTIFICATION_CLICK";

    // Broadcast an ALLE registrierten Geraete - ohne Admin-Schranke waere das ein
    // oeffentlicher Spam-/Phishing-Kanal (Titel/Text kommen 1:1 aus dem Request).
    [MediatorHttpPost("/test-push", OperationId = "SendTestPush", RequiresAuthorization = true, AuthorizationPolicies = [AuthorizationPolicies.RequireAdmin])]
    public async Task<SendTestPushResponse> Handle(
        SendTestPushRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var result = await pushManager.Broadcast(new Shiny.Extensions.Push.PushNotification
        {
            Title = request.Title,
            Message = request.Body,
            DeepLink = ShinyAndroidClickAction,
            Sound = "default",
            Data = new Dictionary<string, string> { ["action"] = "test" },
            Apple = new ApplePushOptions { Category = "test" }
        }, cancellationToken);

        logger.LogInformation(
            "Test push batch {BatchId}: {Sent}/{Total} sent, {Failed} failed, {Pruned} pruned",
            result.BatchId,
            result.Sent,
            result.Total,
            result.Failed,
            result.TokensRemoved);

        var failureReasons = result.Results
            .Where(x => !x.IsSuccess)
            .Select(x => x.Reason ?? x.Status.ToString())
            .Distinct(StringComparer.Ordinal)
            .Take(3)
            .ToArray();
        var detail = failureReasons.Length == 0
            ? string.Empty
            : $" ({string.Join(", ", failureReasons)})";

        return new SendTestPushResponse(
            result.Sent,
            $"Sent {result.Sent}/{result.Total}; failed {result.Failed}; pruned {result.TokensRemoved}{detail}");
    }
}
