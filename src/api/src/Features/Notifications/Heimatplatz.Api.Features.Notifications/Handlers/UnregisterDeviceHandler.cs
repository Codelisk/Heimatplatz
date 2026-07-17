using Heimatplatz.Api;
using Heimatplatz.Api.Features.Notifications.Contracts.Mediator.Requests;
using Shiny;
using Shiny.Extensions.Push;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Notifications.Handlers;

/// <summary>
/// Handler to unregister a device from push notifications.
/// Kein Auth-Zwang: Der vollstaendige Device-Token (bzw. die Subscription-Endpoint-URL)
/// ist nicht erratbar und nur dem Geraet selbst bekannt - sein Besitz ist der Nachweis.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/notifications")]
public class UnregisterDeviceHandler(
    IPushManager pushManager
) : IRequestHandler<UnregisterDeviceRequest, UnregisterDeviceResponse>
{
    [MediatorHttpPost("/unregister-device", OperationId = "UnregisterDevice")]
    public async Task<UnregisterDeviceResponse> Handle(
        UnregisterDeviceRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var platform = ParsePlatform(request.Platform);
        await pushManager.UnregisterDevice(request.DeviceToken, platform, cancellationToken);
        return new UnregisterDeviceResponse(true);
    }

    private static DevicePlatform ParsePlatform(string platform) => platform.ToLowerInvariant() switch
    {
        "ios" => DevicePlatform.iOS,
        "macos" or "maccatalyst" => DevicePlatform.MacOS,
        "android" => DevicePlatform.Android,
        "windows" or "desktop" => DevicePlatform.Windows,
        "web" or "webbrowser" => DevicePlatform.WebBrowser,
        _ => throw new ArgumentException($"Unsupported push platform '{platform}'.", nameof(platform))
    };
}
