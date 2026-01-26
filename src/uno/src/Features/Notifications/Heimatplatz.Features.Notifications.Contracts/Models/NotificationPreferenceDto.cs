namespace Heimatplatz.Features.Notifications.Contracts.Models;

/// <summary>
/// Data transfer object for notification preferences
/// </summary>
/// <param name="IsEnabled">Whether notifications are enabled</param>
/// <param name="FilterMode">The filter mode (All, SameAsSearch, Custom)</param>
/// <param name="Locations">List of locations for custom filter</param>
/// <param name="IsHausSelected">House property type selected</param>
/// <param name="IsGrundstueckSelected">Land property type selected</param>
/// <param name="IsZwangsversteigerungSelected">Foreclosure property type selected</param>
/// <param name="IsPrivateSelected">Private seller type selected</param>
/// <param name="IsBrokerSelected">Broker seller type selected</param>
/// <param name="IsPortalSelected">Portal seller type selected</param>
/// <param name="ExcludedSellerSourceIds">Excluded seller source IDs</param>
public record NotificationPreferenceDto(
    bool IsEnabled,
    NotificationFilterMode FilterMode,
    List<string> Locations,
    bool IsHausSelected = true,
    bool IsGrundstueckSelected = true,
    bool IsZwangsversteigerungSelected = true,
    bool IsPrivateSelected = true,
    bool IsBrokerSelected = true,
    bool IsPortalSelected = true,
    List<Guid>? ExcludedSellerSourceIds = null
);
