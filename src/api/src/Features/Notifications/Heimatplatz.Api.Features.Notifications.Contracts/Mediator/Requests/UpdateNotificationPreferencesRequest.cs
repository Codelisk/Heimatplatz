using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Notifications.Contracts.Mediator.Requests;

/// <summary>
/// Request to update user's notification preferences
/// </summary>
public record UpdateNotificationPreferencesRequest(
    bool IsEnabled,
    NotificationFilterMode FilterMode,
    List<string>? Locations,
    bool IsHausSelected,
    bool IsGrundstueckSelected,
    bool IsZwangsversteigerungSelected,
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
