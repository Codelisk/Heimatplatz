using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Notifications.Contracts.Mediator.Requests;

/// <summary>
/// Request to get user's notification preferences
/// </summary>
public record GetNotificationPreferencesRequest : IRequest<GetNotificationPreferencesResponse>;

/// <summary>
/// Response containing user's notification preferences
/// </summary>
/// <param name="IsEnabled">Whether notifications are enabled</param>
/// <param name="Locations">List of locations user wants notifications for</param>
public record GetNotificationPreferencesResponse(
    bool IsEnabled,
    List<string> Locations
);
