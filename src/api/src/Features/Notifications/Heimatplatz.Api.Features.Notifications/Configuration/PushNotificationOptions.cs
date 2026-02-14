namespace Heimatplatz.Api.Features.Notifications.Configuration;

/// <summary>
/// Configuration options for push notification providers
/// </summary>
public class PushNotificationOptions
{
    public const string SectionName = "PushNotifications";

    /// <summary>
    /// Firebase Cloud Messaging configuration for Android
    /// </summary>
    public FirebaseOptions Firebase { get; set; } = new();

    /// <summary>
    /// Apple Push Notification service configuration for iOS
    /// </summary>
    public ApnsOptions Apns { get; set; } = new();
}

/// <summary>
/// Firebase Cloud Messaging (FCM) configuration
/// </summary>
public class FirebaseOptions
{
    /// <summary>
    /// Path to the Firebase service account JSON file (for local development)
    /// </summary>
    public string? ServiceAccountPath { get; set; }

    /// <summary>
    /// Firebase service account JSON content directly (for Azure/cloud deployment).
    /// Set via environment variable: PushNotifications__Firebase__ServiceAccountJson
    /// </summary>
    public string? ServiceAccountJson { get; set; }

    /// <summary>
    /// Whether Firebase is enabled
    /// </summary>
    public bool Enabled => !string.IsNullOrEmpty(ServiceAccountJson) || !string.IsNullOrEmpty(ServiceAccountPath);
}

/// <summary>
/// Apple Push Notification service (APNs) configuration
/// </summary>
public class ApnsOptions
{
    /// <summary>
    /// Team ID from Apple Developer account
    /// </summary>
    public string? TeamId { get; set; }

    /// <summary>
    /// Key ID for the APNs auth key
    /// </summary>
    public string? KeyId { get; set; }

    /// <summary>
    /// Path to the .p8 private key file (for local development)
    /// </summary>
    public string? PrivateKeyPath { get; set; }

    /// <summary>
    /// .p8 private key content directly (for Azure/cloud deployment).
    /// Set via environment variable: PushNotifications__Apns__PrivateKeyContent
    /// </summary>
    public string? PrivateKeyContent { get; set; }

    /// <summary>
    /// Bundle ID of the iOS app
    /// </summary>
    public string BundleId { get; set; } = "at.heimatplatz.app";

    /// <summary>
    /// Whether to use the production APNs server (false = sandbox)
    /// </summary>
    public bool UseProduction { get; set; } = false;

    /// <summary>
    /// Whether APNs is enabled
    /// </summary>
    public bool Enabled => !string.IsNullOrEmpty(TeamId)
                           && !string.IsNullOrEmpty(KeyId)
                           && (!string.IsNullOrEmpty(PrivateKeyPath) || !string.IsNullOrEmpty(PrivateKeyContent));
}
