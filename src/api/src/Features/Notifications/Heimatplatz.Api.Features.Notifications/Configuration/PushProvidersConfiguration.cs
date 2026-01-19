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
            var jwtContent = File.ReadAllText(options.Apns.PrivateKeyPath!);

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

        if (!string.IsNullOrEmpty(options.ServiceAccountPath) && File.Exists(options.ServiceAccountPath))
        {
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile(options.ServiceAccountPath)
            });
        }
    }
}
