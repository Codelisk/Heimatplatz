namespace Heimatplatz.Features.Notifications.Contracts.Interfaces;

/// <summary>
/// Initializes push notifications after an explicit user action.
/// </summary>
public interface IPushNotificationInitializer
{
    /// <summary>
    /// Requests permission and registers the device token with the API.
    /// This method must only be called in response to an explicit opt-in.
    /// </summary>
    Task<PushInitializationResult> InitializeAsync();
}

public enum PushInitializationStatus
{
    Available,
    Denied,
    Disabled,
    NotConfigured,
    NotSupported,
    Restricted,
    Failed
}

public sealed record PushInitializationResult(PushInitializationStatus Status)
{
    public bool IsAvailable => Status == PushInitializationStatus.Available;
}
