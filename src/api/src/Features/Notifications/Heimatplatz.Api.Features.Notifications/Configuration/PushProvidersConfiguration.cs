using Heimatplatz.Api.Features.Notifications.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Extensions.Push;
using Shiny.Extensions.Push.Apns;
using Shiny.Extensions.Push.Fcm;
using Shiny.Extensions.Push.WebPush;

namespace Heimatplatz.Api.Features.Notifications.Configuration;

/// <summary>
/// Configures Shiny.Extensions.Push with the Heimatplatz EF registration repository.
/// </summary>
public static class PushProvidersConfiguration
{
    public static IServiceCollection AddPushProviders(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration
            .GetSection(PushNotificationOptions.SectionName)
            .Get<PushNotificationOptions>() ?? new PushNotificationOptions();

        services.Configure<PushNotificationOptions>(
            configuration.GetSection(PushNotificationOptions.SectionName));

        var firebaseCredentialPath = ResolveCredentialPath(options.Firebase.ServiceAccountPath);
        var apnsPrivateKeyPath = ResolveCredentialPath(options.Apns.PrivateKeyPath);

        services.AddPushNotifications(push =>
        {
            // FCM HTTP v1 no longer supports the multipart /batch endpoint used by
            // Shiny.Extensions.Push 3.0.0-beta-0002. A batch-level 404 is otherwise
            // interpreted as expired tokens and prunes valid Android subscriptions.
            // Per-device sends use the supported messages:send endpoint instead.
            push.Configure(manager => manager.EnableBatching = false);
            push.UseRepository<EfPushRepository>();

            if (options.Firebase.Enabled
                && (!string.IsNullOrWhiteSpace(options.Firebase.ServiceAccountJson)
                    || firebaseCredentialPath is not null))
            {
                push.AddFcm(fcm =>
                {
                    fcm.ServiceAccountJson = options.Firebase.ServiceAccountJson;
                    fcm.ServiceAccountJsonPath = firebaseCredentialPath;
                });
            }

            if (options.Apns.Enabled
                && (!string.IsNullOrWhiteSpace(options.Apns.PrivateKeyContent)
                    || apnsPrivateKeyPath is not null))
            {
                push.AddApns(apns =>
                {
                    apns.TeamId = options.Apns.TeamId!;
                    apns.KeyId = options.Apns.KeyId!;
                    apns.BundleId = options.Apns.BundleId;

                    if (!string.IsNullOrWhiteSpace(options.Apns.PrivateKeyContent))
                    {
                        apns.PrivateKey = NormalizePrivateKey(options.Apns.PrivateKeyContent);
                    }
                    else
                    {
                        apns.PrivateKeyPath = apnsPrivateKeyPath;
                    }
                });
            }

            if (options.WebPush.Enabled)
            {
                push.AddWebPush(web =>
                {
                    web.PublicKey = options.WebPush.PublicKey!;
                    web.PrivateKey = options.WebPush.PrivateKey!;
                    web.Subject = options.WebPush.Subject;
                });
            }
        });

        return services;
    }

    private static string? ResolveCredentialPath(string? configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
            return null;

        if (Path.IsPathRooted(configuredPath))
            return File.Exists(configuredPath) ? configuredPath : null;

        var outputPath = Path.Combine(AppContext.BaseDirectory, configuredPath);
        if (File.Exists(outputPath))
            return outputPath;

        var workingDirectoryPath = Path.GetFullPath(configuredPath);
        return File.Exists(workingDirectoryPath) ? workingDirectoryPath : null;
    }

    private static string NormalizePrivateKey(string key)
    {
        var trimmed = key.Trim();
        if (trimmed.Contains("BEGIN PRIVATE KEY", StringComparison.Ordinal))
            return trimmed;

        return $"-----BEGIN PRIVATE KEY-----\n{trimmed}\n-----END PRIVATE KEY-----";
    }
}
