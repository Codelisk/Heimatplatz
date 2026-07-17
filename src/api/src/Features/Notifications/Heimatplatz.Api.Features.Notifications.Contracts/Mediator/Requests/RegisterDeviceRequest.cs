using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Notifications.Contracts.Mediator.Requests;

/// <summary>
/// Request to register a device for push notifications
/// </summary>
/// <param name="DeviceToken">Push notification device token (for Web: the push subscription endpoint URL)</param>
/// <param name="Platform">Platform (iOS, Android, Desktop, Web)</param>
/// <param name="Environment">Push environment (Production or Sandbox)</param>
/// <param name="DeviceId">Stable app-installation identifier used across token rotations</param>
/// <param name="Data">Provider-specific registration data (for Web: the "p256dh" and "auth" subscription keys)</param>
public record RegisterDeviceRequest(
    string DeviceToken,
    string Platform,
    string? Environment = null,
    string? DeviceId = null,
    Dictionary<string, string>? Data = null
) : IRequest<RegisterDeviceResponse>;

/// <summary>
/// Response after registering device
/// </summary>
/// <param name="Success">Whether registration was successful</param>
public record RegisterDeviceResponse(bool Success);
