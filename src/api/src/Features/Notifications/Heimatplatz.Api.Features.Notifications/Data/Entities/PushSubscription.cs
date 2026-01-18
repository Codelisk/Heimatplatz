using Heimatplatz.Api.Core.Data.Entities;
using Heimatplatz.Api.Features.Auth.Data.Entities;

namespace Heimatplatz.Api.Features.Notifications.Data.Entities;

/// <summary>
/// Stores device tokens for push notification delivery
/// </summary>
public class PushSubscription : BaseEntity
{
    /// <summary>User ID this subscription belongs to</summary>
    public required Guid UserId { get; set; }

    /// <summary>Push notification device token</summary>
    public required string DeviceToken { get; set; }

    /// <summary>Platform (iOS, Android, Desktop, Web)</summary>
    public required string Platform { get; set; }

    /// <summary>When this subscription was created</summary>
    public DateTimeOffset SubscribedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Navigation property to User</summary>
    public User User { get; set; } = null!;
}
