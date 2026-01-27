namespace Heimatplatz.Features.Notifications.Services;

/// <summary>
/// Push notification access state
/// </summary>
public enum PushAccessState
{
    Unknown,
    Available,
    Denied,
    Disabled,
    NotSupported,
    Restricted
}

/// <summary>
/// Result of requesting push notification access
/// </summary>
public record PushAccessResult(PushAccessState Status, string? RegistrationToken);

/// <summary>
/// Interface for managing push notifications (platform-agnostic)
/// </summary>
public interface IFirebasePushManager
{
    /// <summary>
    /// Current FCM registration token
    /// </summary>
    string? CurrentToken { get; }

    /// <summary>
    /// Requests push notification access and returns the registration token
    /// </summary>
    Task<PushAccessResult> RequestAccessAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Event fired when a new token is received
    /// </summary>
    event EventHandler<string>? TokenReceived;

    /// <summary>
    /// Event fired when a push message is received while app is in foreground
    /// </summary>
    event EventHandler<PushNotificationEventArgs>? MessageReceived;
}

/// <summary>
/// Event args for push notification received
/// </summary>
public class PushNotificationEventArgs(string title, string body, IDictionary<string, string> data) : EventArgs
{
    public string Title { get; } = title;
    public string Body { get; } = body;
    public IDictionary<string, string> Data { get; } = data;
}
