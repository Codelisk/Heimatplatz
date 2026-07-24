using Heimatplatz.Maui.Events;
using Heimatplatz.Maui.Features.Auth;
using Heimatplatz.Maui.Features.Properties.Sync;
using Heimatplatz.Maui.Features.Telemetry.Services;
using Heimatplatz.Maui.Offline;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Maui.Services;

/// <summary>
/// App-Startlogik (ersetzt das ShellViewModel der Uno-App):
/// Session-Restore, Synchronisierung und Logout-Handling via Mediator-Event.
/// </summary>
[Singleton(AsSelf = true)]
public class AppStartupService(
    IAuthService authService,
    IMediator mediator,
    INavigator navigator,
    PropertySyncService propertySync,
    TelemetryLogSender telemetrySender,
    ILogger<AppStartupService> logger
) : IEventHandler<LogoutRequestedEvent>
{
    /// <summary>
    /// Wird beim App-Start (nach Erstellung des Windows) aufgerufen.
    /// </summary>
    public async Task StartAsync()
    {
#if !ANDROID
        // Diese Abhaengigkeiten werden nur vom Android-In-App-Updatepfad benoetigt.
        _ = mediator;
        _ = logger;
#endif

        // Crash-Reports des letzten Laufs melden (fire-and-forget, fail-open)
        _ = telemetrySender.SendPendingCrashReportsAsync();

        // Immobilien-Delta-Sync: haelt die lokalen Caches aktuell, solange die App laeuft
        propertySync.Start();

        // Ein Session-Restore darf keinen Betriebssystem-Prompt ausloesen.
        // Push wird ausschliesslich ueber den Schalter in den Einstellungen aktiviert.
        await authService.TryRestoreSessionAsync();

#if ANDROID
        // In-App-Update-Check (fire-and-forget wie in der Uno-App)
        try
        {
            await mediator.Send(new Heimatplatz.Features.AppUpdate.Contracts.Mediator.Commands.CheckForAppUpdateCommand());
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "App-Update-Check fehlgeschlagen");
        }
#endif
    }

    /// <summary>
    /// App kehrt aus dem Hintergrund zurueck: sofort einen Delta-Sync anstossen.
    /// </summary>
    public void OnAppResumed() => propertySync.TriggerSyncNow();

    /// <summary>
    /// Logout: Auth-State bereinigen und zur Login-Seite navigieren.
    /// </summary>
    public async Task Handle(LogoutRequestedEvent @event, IMediatorContext context, CancellationToken cancellationToken)
    {
        var userCachePrefix = UserScopedContractKeyProvider.GetScopePrefix(authService.UserId);
        await context.FlushStores(
            userCachePrefix,
            partialMatch: true,
            cancellationToken: cancellationToken);
        authService.ClearAuthentication();
        await navigator.NavigateTo("Login", relativeNavigation: false);
    }
}
