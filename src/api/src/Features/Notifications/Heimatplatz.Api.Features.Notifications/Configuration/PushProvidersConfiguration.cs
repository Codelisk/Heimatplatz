using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Heimatplatz.Api.Features.Notifications.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Heimatplatz.Api.Features.Notifications.Configuration;

/// <summary>
/// Configuration for push notification providers (Firebase + APNs)
/// </summary>
public static class PushProvidersConfiguration
{
    /// <summary>
    /// Adds and configures push notification providers
    /// </summary>
    public static IServiceCollection AddPushProviders(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration
            .GetSection(PushNotificationOptions.SectionName)
            .Get<PushNotificationOptions>() ?? new PushNotificationOptions();

        services.Configure<PushNotificationOptions>(
            configuration.GetSection(PushNotificationOptions.SectionName));

        // Initialize Firebase if configured
        if (options.Firebase.Enabled)
        {
            InitializeFirebase(options.Firebase);
        }

        // Register APNs service if configured
        if (options.Apns.Enabled)
        {
            // Resolve key content: Priority 1 = direct content, Priority 2 = file
            string? keyContent = null;

            if (!string.IsNullOrEmpty(options.Apns.PrivateKeyContent))
            {
                keyContent = options.Apns.PrivateKeyContent;
            }
            else if (!string.IsNullOrEmpty(options.Apns.PrivateKeyPath) && File.Exists(options.Apns.PrivateKeyPath))
            {
                keyContent = File.ReadAllText(options.Apns.PrivateKeyPath);
            }

            if (!string.IsNullOrEmpty(keyContent))
            {
                // Normalize to raw Base64
                keyContent = keyContent
                    .Replace("-----BEGIN PRIVATE KEY-----", "")
                    .Replace("-----END PRIVATE KEY-----", "")
                    .Replace("\n", "")
                    .Replace("\r", "")
                    .Trim();

                var capturedKey = keyContent;
                var capturedOptions = options.Apns;

                services.AddSingleton<IApnsService>(sp =>
                    new ApnsService(
                        capturedOptions,
                        capturedKey,
                        sp.GetRequiredService<ILogger<ApnsService>>()));
            }
        }

        return services;
    }

    private static void InitializeFirebase(FirebaseOptions options)
    {
        if (FirebaseApp.DefaultInstance != null)
            return;

        GoogleCredential? credential = null;

        if (!string.IsNullOrEmpty(options.ServiceAccountJson))
        {
            credential = GoogleCredential.FromJson(options.ServiceAccountJson);
        }
        else if (!string.IsNullOrEmpty(options.ServiceAccountPath) && File.Exists(options.ServiceAccountPath))
        {
            credential = GoogleCredential.FromFile(options.ServiceAccountPath);
        }

        if (credential != null)
        {
            FirebaseApp.Create(new AppOptions { Credential = credential });
        }
    }
}
