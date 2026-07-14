using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Heimatplatz.Api;
using Heimatplatz.Api.Features.Notifications.Contracts.Mediator.Requests;
using Microsoft.AspNetCore.Http;
using Shiny;
using Shiny.Extensions.Push;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Notifications.Handlers;

/// <summary>
/// Handler to register a device for push notifications
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/notifications")]
public class RegisterDeviceHandler(
    IHttpContextAccessor httpContextAccessor,
    IPushManager pushManager
) : IRequestHandler<RegisterDeviceRequest, RegisterDeviceResponse>
{
    [MediatorHttpPost("/register-device", OperationId = "RegisterDevice")]
    public async Task<RegisterDeviceResponse> Handle(
        RegisterDeviceRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        // Get user ID from JWT token
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext is not available");

        var userIdClaim = httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)
            ?? throw new UnauthorizedAccessException("User ID not found in token");

        if (!Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid User ID in token");
        }

        var platform = ParsePlatform(request.Platform);
        var environment = ParseEnvironment(request.Environment);

        await pushManager.RegisterDevice(new DeviceRegistration
        {
            DeviceToken = request.DeviceToken,
            DeviceId = request.DeviceId,
            UserIdentifier = userId.ToString(),
            Platform = platform,
            Environment = environment
        }, cancellationToken);

        return new RegisterDeviceResponse(true);
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

    private static PushEnvironment ParseEnvironment(string? environment)
    {
        if (string.IsNullOrWhiteSpace(environment))
            return PushEnvironment.Production;

        return Enum.TryParse<PushEnvironment>(environment, ignoreCase: true, out var parsed)
            ? parsed
            : throw new ArgumentException($"Unsupported push environment '{environment}'.", nameof(environment));
    }
}
