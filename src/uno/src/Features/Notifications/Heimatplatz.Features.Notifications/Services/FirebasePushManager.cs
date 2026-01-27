using Microsoft.Extensions.Logging;

namespace Heimatplatz.Features.Notifications.Services;

#if __ANDROID__
using Android;
using Android.Content.PM;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Firebase.Messaging;

/// <summary>
/// Firebase Cloud Messaging push manager for Android
/// </summary>
[Service(UnoService.Lifetime, TryAdd = UnoService.TryAdd)]
public class FirebasePushManager : IFirebasePushManager
{
    private readonly ILogger<FirebasePushManager> _logger;
    private readonly INotificationService _notificationService;

    /// <summary>
    /// Static token storage for access from FirebaseMessagingService
    /// </summary>
    public static string? CurrentToken { get; set; }

    string? IFirebasePushManager.CurrentToken => CurrentToken;

    public event EventHandler<string>? TokenReceived;
    public event EventHandler<PushNotificationEventArgs>? MessageReceived;

    public FirebasePushManager(
        ILogger<FirebasePushManager> logger,
        INotificationService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<PushAccessResult> RequestAccessAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Requesting FCM push notification access...");

            // Check Android 13+ POST_NOTIFICATIONS permission
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Tiramisu)
            {
                var context = Android.App.Application.Context;
                var permission = ContextCompat.CheckSelfPermission(context, Manifest.Permission.PostNotifications);

                if (permission != Permission.Granted)
                {
                    _logger.LogWarning("POST_NOTIFICATIONS permission not granted");
                    return new PushAccessResult(PushAccessState.Denied, null);
                }
            }

            // Get the FCM token
            var token = await GetTokenAsync();

            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Failed to get FCM token");
                return new PushAccessResult(PushAccessState.Disabled, null);
            }

            CurrentToken = token;
            _logger.LogInformation("FCM token obtained: {Token}", token);

            // Register the token with our API
            await RegisterTokenWithApiAsync(token, cancellationToken);

            return new PushAccessResult(PushAccessState.Available, token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting FCM access");
            return new PushAccessResult(PushAccessState.Unknown, null);
        }
    }

    private async Task<string?> GetTokenAsync()
    {
        try
        {
            var tokenTask = FirebaseMessaging.Instance.GetToken();
            var token = await tokenTask.AsAsync<Java.Lang.String>();
            return token?.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting FCM token");
            return null;
        }
    }

    private async Task RegisterTokenWithApiAsync(string token, CancellationToken cancellationToken)
    {
        try
        {
            var success = await _notificationService.RegisterDeviceAsync(token, "Android", cancellationToken);
            if (success)
            {
                _logger.LogInformation("Device token registered successfully with API");
            }
            else
            {
                _logger.LogWarning("Failed to register device token with API");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering device token with API");
        }
    }

    /// <summary>
    /// Called by FirebaseMessagingService when a new token is received
    /// </summary>
    internal void OnTokenRefreshed(string token)
    {
        _logger.LogInformation("FCM token refreshed: {Token}", token);
        CurrentToken = token;
        TokenReceived?.Invoke(this, token);

        // Register the new token with API (fire and forget)
        _ = RegisterTokenWithApiAsync(token, CancellationToken.None);
    }

    /// <summary>
    /// Called by FirebaseMessagingService when a message is received
    /// </summary>
    internal void OnMessageReceived(string title, string body, IDictionary<string, string> data)
    {
        _logger.LogInformation("Push message received: {Title}", title);
        MessageReceived?.Invoke(this, new PushNotificationEventArgs(title, body, data));
    }
}

#else
// Stub implementation for non-Android platforms
[Service(UnoService.Lifetime, TryAdd = UnoService.TryAdd)]
public class FirebasePushManager(ILogger<FirebasePushManager> logger) : IFirebasePushManager
{
    public string? CurrentToken => null;

    public event EventHandler<string>? TokenReceived;
    public event EventHandler<PushNotificationEventArgs>? MessageReceived;

    public Task<PushAccessResult> RequestAccessAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Push notifications are not supported on this platform");
        return Task.FromResult(new PushAccessResult(PushAccessState.NotSupported, null));
    }
}
#endif
