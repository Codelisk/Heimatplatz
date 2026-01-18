using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Notifications.Contracts.Mediator.Requests;

/// <summary>
/// Request to register a device for push notifications
/// </summary>
/// <param name="DeviceToken">Push notification device token</param>
/// <param name="Platform">Platform (iOS, Android, Desktop, Web)</param>
public record RegisterDeviceRequest(
    string DeviceToken,
    string Platform
) : IRequest<RegisterDeviceResponse>;

/// <summary>
/// Response after registering device
/// </summary>
/// <param name="Success">Whether registration was successful</param>
public record RegisterDeviceResponse(bool Success);
