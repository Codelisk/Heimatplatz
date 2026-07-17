using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Notifications.Contracts.Mediator.Requests;

/// <summary>
/// Request to unregister a device from push notifications
/// </summary>
/// <param name="DeviceToken">Push notification device token (for Web: the push subscription endpoint URL)</param>
/// <param name="Platform">Platform (iOS, Android, Desktop, Web)</param>
public record UnregisterDeviceRequest(
    string DeviceToken,
    string Platform
) : IRequest<UnregisterDeviceResponse>;

/// <summary>
/// Response after unregistering a device
/// </summary>
/// <param name="Success">Whether a matching registration was removed</param>
public record UnregisterDeviceResponse(bool Success);
