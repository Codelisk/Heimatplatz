using Heimatplatz.Api;
using Heimatplatz.Api.Features.Notifications.Configuration;
using Heimatplatz.Api.Features.Notifications.Contracts.Mediator.Requests;
using Microsoft.Extensions.Options;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Notifications.Handlers;

/// <summary>
/// Handler that serves the VAPID public key browsers need to create a Web Push subscription.
/// Der Public Key ist per Definition oeffentlich - kein Auth noetig.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/notifications")]
public class GetWebPushPublicKeyHandler(
    IOptions<PushNotificationOptions> options
) : IRequestHandler<GetWebPushPublicKeyRequest, GetWebPushPublicKeyResponse>
{
    [MediatorHttpGet("/web-push-public-key", OperationId = "GetWebPushPublicKey")]
    public Task<GetWebPushPublicKeyResponse> Handle(
        GetWebPushPublicKeyRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var webPush = options.Value.WebPush;
        return Task.FromResult(new GetWebPushPublicKeyResponse(
            webPush.Enabled,
            webPush.Enabled ? webPush.PublicKey : null));
    }
}
