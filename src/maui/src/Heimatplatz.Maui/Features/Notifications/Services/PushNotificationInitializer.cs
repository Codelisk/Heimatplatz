using System.Diagnostics;
using Heimatplatz.Features.Notifications.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
#if ANDROID || IOS
using Shiny;
using Shiny.Push;
#endif

namespace Heimatplatz.Maui.Features.Notifications.Services;

#if ANDROID || IOS
/// <summary>
/// Initialisiert Push Notifications via Shiny.Push (Android FCM / iOS APNs).
/// Fordert die Berechtigung an und registriert das Geraet beim API.
/// </summary>
public class PushNotificationInitializer(
    IPushManager pushManager,
    INotificationService notificationService,
    ILogger<PushNotificationInitializer> logger) : IPushNotificationInitializer
{
    private readonly object initializationGate = new();
    private Task<PushInitializationResult>? activeInitialization;

    /// <summary>
    /// Maximale Wartezeit fuer plattformabhaengige Aufrufe (iOS APNs-Registrierung, API-Call).
    /// Verhindert, dass die UI (z. B. "Registrierung wird durchgefuehrt...") unbegrenzt blockiert,
    /// falls der APNs-Callback nie zurueckkommt. Ein try/catch faengt einen Hang NICHT ab.
    /// </summary>
    private static readonly TimeSpan InitTimeout = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Initialisiert Push nach einer ausdruecklichen Nutzeraktion in den Einstellungen.
    /// </summary>
    public Task<PushInitializationResult> InitializeAsync()
    {
        lock (initializationGate)
        {
            if (activeInitialization is { IsCompleted: false })
            {
                logger.LogDebug("[PushNotificationInitializer] Push initialization already in progress");
                return activeInitialization;
            }

            activeInitialization = InitializeCoreAsync();
            return activeInitialization;
        }
    }

    private async Task<PushInitializationResult> InitializeCoreAsync()
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            logger.LogInformation("[PushNotificationInitializer] Initializing push notifications...");

            // RequestAccess() wartet auf den iOS-APNs-Registrierungs-Callback, der ohne korrekte
            // Push-Provisionierung nie feuert -> harte Obergrenze per WaitAsync.
            var result = await pushManager.RequestAccess().WaitAsync(InitTimeout);

            switch (result.Status)
            {
                case AccessState.Available:
                    logger.LogInformation("[PushNotificationInitializer] Push notifications enabled. Token: {Token}",
                        MaskToken(result.RegistrationToken));

                    // Register token with API (OnNewToken is only called on token change, not on every RequestAccess)
                    if (string.IsNullOrEmpty(result.RegistrationToken))
                    {
                        logger.LogWarning("[PushNotificationInitializer] No registration token was returned");
                        return new(PushInitializationStatus.Failed);
                    }

                    var platform = GetCurrentPlatform();
                    var success = await notificationService.RegisterDeviceAsync(result.RegistrationToken, platform)
                        .WaitAsync(InitTimeout);
                    if (!success)
                    {
                        logger.LogWarning("[PushNotificationInitializer] Failed to register device with API");
                        return new(PushInitializationStatus.Failed);
                    }

                    logger.LogInformation("[PushNotificationInitializer] Device registered successfully with API");
                    return new(PushInitializationStatus.Available);

                case AccessState.Denied:
                    logger.LogWarning("[PushNotificationInitializer] Push notification permission denied by user");
                    return new(PushInitializationStatus.Denied);

                case AccessState.Disabled:
                    logger.LogWarning("[PushNotificationInitializer] Push notifications are disabled on this device");
                    return new(PushInitializationStatus.Disabled);

                case AccessState.NotSetup:
                    logger.LogWarning("[PushNotificationInitializer] Push notifications are not properly configured");
                    return new(PushInitializationStatus.NotConfigured);

                case AccessState.NotSupported:
                    logger.LogWarning("[PushNotificationInitializer] Push notifications are not supported on this platform");
                    return new(PushInitializationStatus.NotSupported);

                case AccessState.Restricted:
                    logger.LogWarning("[PushNotificationInitializer] Push notifications are restricted (parental controls?)");
                    return new(PushInitializationStatus.Restricted);

                default:
                    logger.LogWarning("[PushNotificationInitializer] Unknown push notification status: {Status}", result.Status);
                    return new(PushInitializationStatus.Failed);
            }
        }
        catch (TimeoutException)
        {
            // Push-Registrierung haengt (z. B. fehlende APNs-Provisionierung) - UI nicht blockieren.
            logger.LogWarning(
                "[PushNotificationInitializer] Push initialization timed out after {ElapsedMs}ms and was skipped",
                stopwatch.ElapsedMilliseconds);
            return new(PushInitializationStatus.Failed);
        }
        catch (OperationCanceledException)
        {
            // Shiny.Push beendet seinen internen APNs-Tokenabruf aktuell selbst per
            // Cancellation. Das ist funktional ein Timeout und kein App-Fehler.
            logger.LogWarning(
                "[PushNotificationInitializer] Push initialization was canceled after {ElapsedMs}ms and was skipped",
                stopwatch.ElapsedMilliseconds);
            return new(PushInitializationStatus.Failed);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[PushNotificationInitializer] Failed to initialize push notifications");
            return new(PushInitializationStatus.Failed);
        }
    }

    /// <summary>
    /// Gets the current platform identifier
    /// </summary>
    private static string GetCurrentPlatform()
    {
#if ANDROID
        return "Android";
#else
        return "iOS";
#endif
    }

    private static string? MaskToken(string? token)
        => string.IsNullOrEmpty(token) || token.Length <= 8
            ? "***"
            : $"{token[..4]}…{token[^4..]}";
}
#else
/// <summary>
/// No-Op-Implementierung fuer Plattformen ohne Push-Support (Windows/MacCatalyst).
/// </summary>
public class PushNotificationInitializer(ILogger<PushNotificationInitializer> logger) : IPushNotificationInitializer
{
    /// <summary>
    /// No-op on platforms that don't support push notifications
    /// </summary>
    public Task<PushInitializationResult> InitializeAsync()
    {
        logger.LogInformation("[PushNotificationInitializer] Push notifications are not supported on this platform");
        return Task.FromResult(new PushInitializationResult(PushInitializationStatus.NotSupported));
    }
}
#endif
