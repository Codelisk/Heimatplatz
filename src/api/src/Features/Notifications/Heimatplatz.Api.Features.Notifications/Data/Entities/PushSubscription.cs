using Heimatplatz.Api.Core.Data.Entities;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Shiny.Extensions.Push;

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

    /// <summary>Stable app-installation identifier, independent of token rotation</summary>
    public string? DeviceId { get; set; }

    /// <summary>Application/provider key for multi-app push configurations</summary>
    public string? AppId { get; set; }

    /// <summary>APNs/FCM environment associated with this token</summary>
    public PushEnvironment Environment { get; set; } = PushEnvironment.Production;

    /// <summary>Serialized Shiny push tags</summary>
    public string TagsJson { get; set; } = "[]";

    /// <summary>Serialized Shiny push topic subscriptions</summary>
    public string TopicsJson { get; set; } = "[]";

    /// <summary>Optional device locale</summary>
    public string? Locale { get; set; }

    /// <summary>Optional client app version</summary>
    public string? AppVersion { get; set; }

    /// <summary>Optional registration expiry/last-seen marker</summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>Serialized provider-specific registration data</summary>
    public string? DataJson { get; set; }

    /// <summary>When this subscription was created</summary>
    public DateTimeOffset SubscribedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Navigation property to User</summary>
    public User User { get; set; } = null!;
}
