using Heimatplatz.Api.Core.Data.Entities;
using Heimatplatz.Api.Features.Auth.Data.Entities;

namespace Heimatplatz.Api.Features.Notifications.Data.Entities;

/// <summary>
/// Stores user notification preferences for location-based alerts
/// </summary>
public class NotificationPreference : BaseEntity
{
    /// <summary>User ID this preference belongs to</summary>
    public required Guid UserId { get; set; }

    /// <summary>City/location to receive notifications for</summary>
    public required string Location { get; set; }

    /// <summary>Whether notifications are enabled for this location</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Whether private sellers are included in notifications</summary>
    public bool IsPrivateSelected { get; set; } = true;

    /// <summary>Whether broker sellers are included in notifications</summary>
    public bool IsBrokerSelected { get; set; } = true;

    /// <summary>Whether portal sources are included in notifications</summary>
    public bool IsPortalSelected { get; set; } = true;

    /// <summary>JSON array of excluded SellerSource IDs for notifications</summary>
    public string ExcludedSellerSourceIdsJson { get; set; } = "[]";

    /// <summary>Navigation property to User</summary>
    public User User { get; set; } = null!;
}
