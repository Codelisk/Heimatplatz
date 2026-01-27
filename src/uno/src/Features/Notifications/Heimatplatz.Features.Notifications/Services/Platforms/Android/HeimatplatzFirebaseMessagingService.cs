#if __ANDROID__
using Android.App;
using Android.Content;
using Firebase.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Heimatplatz.Features.Notifications.Services.Platforms.Android;

/// <summary>
/// Firebase Cloud Messaging service for handling push notifications on Android
/// </summary>
[Service(Exported = true)]
[IntentFilter(["com.google.firebase.MESSAGING_EVENT"])]
public class HeimatplatzFirebaseMessagingService : FirebaseMessagingService
{
    /// <summary>
    /// Called when a new FCM token is generated
    /// </summary>
    public override void OnNewToken(string token)
    {
        base.OnNewToken(token);

        // Get services from the app's service provider
        var serviceProvider = GetServiceProvider();
        if (serviceProvider == null) return;

        var logger = serviceProvider.GetService<ILogger<HeimatplatzFirebaseMessagingService>>();
        logger?.LogInformation("New FCM token received: {Token}", token);

        // Store the token for later registration
        FirebasePushManager.CurrentToken = token;

        // Notify the push manager about the new token
        var pushManager = serviceProvider.GetService<IFirebasePushManager>();
        if (pushManager is FirebasePushManager manager)
        {
            manager.OnTokenRefreshed(token);
        }
    }

    /// <summary>
    /// Called when a message is received while app is in foreground
    /// </summary>
    public override void OnMessageReceived(RemoteMessage message)
    {
        base.OnMessageReceived(message);

        var serviceProvider = GetServiceProvider();
        if (serviceProvider == null) return;

        var logger = serviceProvider.GetService<ILogger<HeimatplatzFirebaseMessagingService>>();
        logger?.LogInformation("FCM message received from: {From}", message.From);

        // Extract notification data
        var data = new Dictionary<string, string>();
        foreach (var kvp in message.Data)
        {
            data[kvp.Key] = kvp.Value;
        }

        // Get notification content
        var notification = message.GetNotification();
        var title = notification?.Title ?? data.GetValueOrDefault("title", "Notification");
        var body = notification?.Body ?? data.GetValueOrDefault("body", "");

        logger?.LogInformation("FCM notification: {Title} - {Body}", title, body);

        // Notify the push manager
        var pushManager = serviceProvider.GetService<IFirebasePushManager>();
        if (pushManager is FirebasePushManager manager)
        {
            manager.OnMessageReceived(title, body, data);
        }

        // Show local notification if app is in foreground
        ShowLocalNotification(title, body, data);
    }

    private void ShowLocalNotification(string title, string body, IDictionary<string, string> data)
    {
        var notificationManager = (NotificationManager?)GetSystemService(NotificationService);
        if (notificationManager == null) return;

        // Create notification channel for Android 8.0+
        const string channelId = "heimatplatz_push";
        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(channelId, "Heimatplatz Notifications", NotificationImportance.Default)
            {
                Description = "Push notifications from Heimatplatz"
            };
            notificationManager.CreateNotificationChannel(channel);
        }

        // Create intent for notification tap
        var intent = PackageManager?.GetLaunchIntentForPackage(PackageName ?? "");
        if (intent != null)
        {
            intent.AddFlags(ActivityFlags.ClearTop);
            foreach (var kvp in data)
            {
                intent.PutExtra(kvp.Key, kvp.Value);
            }
        }

        var pendingIntent = intent != null
            ? PendingIntent.GetActivity(this, 0, intent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable)
            : null;

        var notificationBuilder = new Notification.Builder(this, channelId)
            .SetSmallIcon(Android.Resource.Drawable.IcDialogInfo)
            .SetContentTitle(title)
            .SetContentText(body)
            .SetAutoCancel(true);

        if (pendingIntent != null)
        {
            notificationBuilder.SetContentIntent(pendingIntent);
        }

        notificationManager.Notify(DateTime.Now.Millisecond, notificationBuilder.Build());
    }

    private static IServiceProvider? GetServiceProvider()
    {
        // Access the app's service provider through the Application instance
        var app = Application.Context as Microsoft.UI.Xaml.NativeApplication;
        return (app?.Application as App)?.Host?.Services;
    }
}
#endif
