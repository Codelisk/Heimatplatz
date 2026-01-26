using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Notifications.Contracts.Mediator.Requests;

/// <summary>
/// Request to update user's notification preferences
/// </summary>
/// <param name="IsEnabled">Enable/disable notifications</param>
/// <param name="Locations">List of locations to filter notifications by</param>
public record UpdateNotificationPreferencesRequest(
    bool IsEnabled,
    List<string> Locations,
    bool IsPrivateSelected,
    bool IsBrokerSelected,
    bool IsPortalSelected,
    List<Guid>? ExcludedSellerSourceIds
) : IRequest<UpdateNotificationPreferencesResponse>;

/// <summary>
/// Response after updating notification preferences
/// </summary>
/// <param name="Success">Whether the update was successful</param>
public record UpdateNotificationPreferencesResponse(bool Success);
