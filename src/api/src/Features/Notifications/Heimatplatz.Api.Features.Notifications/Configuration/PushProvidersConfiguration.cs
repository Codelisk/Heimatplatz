using Fitomad.Apns;
using Fitomad.Apns.Entities;
using Fitomad.Apns.Entities.Settings;
using Fitomad.Apns.Extensions;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        // Register APNs client if configured
        if (options.Apns.Enabled)
        {
            // Priority 1: Direct key content (for Azure/cloud deployment via environment variable)
            // Priority 2: File path (for local development)
            var jwtContent = !string.IsNullOrEmpty(options.Apns.PrivateKeyContent)
                ? options.Apns.PrivateKeyContent
                : File.ReadAllText(options.Apns.PrivateKeyPath!);

            var jwtToken = new ApnsJsonToken
            {
                Content = jwtContent,
                KeyId = options.Apns.KeyId!,
                TeamId = options.Apns.TeamId!
            };

            var apnsSettings = new ApnsSettingsBuilder()
                .InEnvironment(options.Apns.UseProduction ? ApnsEnvironment.Production : ApnsEnvironment.Development)
                .SetTopic(options.Apns.BundleId)
                .WithJsonToken(jwtToken)
                .Build();

            // Required for JWT token caching
            services.AddDistributedMemoryCache();
            services.AddApns(settings: apnsSettings);
        }

        return services;
    }

    private static void InitializeFirebase(FirebaseOptions options)
    {
        if (FirebaseApp.DefaultInstance != null)
            return;

        GoogleCredential? credential = null;

        // Priority 1: Direct JSON content (for Azure/cloud deployment via environment variable)
        if (!string.IsNullOrEmpty(options.ServiceAccountJson))
        {
            credential = GoogleCredential.FromJson(options.ServiceAccountJson);
        }
        // Priority 2: File path (for local development)
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
