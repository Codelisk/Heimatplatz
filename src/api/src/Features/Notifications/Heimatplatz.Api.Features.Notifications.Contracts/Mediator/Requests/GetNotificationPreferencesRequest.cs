using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Notifications.Contracts.Mediator.Requests;

/// <summary>
/// Request to get user's notification preferences
/// </summary>
public record GetNotificationPreferencesRequest : IRequest<GetNotificationPreferencesResponse>;

/// <summary>
/// Response containing user's notification preferences
/// </summary>
public record GetNotificationPreferencesResponse(
    bool IsEnabled,
    NotificationFilterMode FilterMode,
    List<string> Locations,
    bool IsHausSelected,
    bool IsGrundstueckSelected,
    bool IsZwangsversteigerungSelected,
    bool IsPrivateSelected,
    bool IsBrokerSelected,
    bool IsPortalSelected,
    List<Guid> ExcludedSellerSourceIds
);
