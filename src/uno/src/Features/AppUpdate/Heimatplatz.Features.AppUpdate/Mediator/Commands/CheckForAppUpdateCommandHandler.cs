using Heimatplatz.Features.AppUpdate.Contracts;
using Heimatplatz.Features.AppUpdate.Contracts.Mediator.Commands;
using Microsoft.Extensions.Logging;
using Shiny.Mediator;

namespace Heimatplatz.Features.AppUpdate.Mediator.Commands;

/// <summary>
/// Handles the <see cref="CheckForAppUpdateCommand"/> by checking for updates
/// and starting the appropriate update flow.
/// </summary>
public sealed class CheckForAppUpdateCommandHandler(
    IAppUpdateService updateService,
    ILogger<CheckForAppUpdateCommandHandler> logger)
    : ICommandHandler<CheckForAppUpdateCommand>
{
    public async Task Handle(CheckForAppUpdateCommand command, IMediatorContext context, CancellationToken cancellationToken)
    {
        var updateInfo = await updateService.CheckForUpdateAsync();
        if (updateInfo?.IsUpdateAvailable != true)
        {
            logger.LogDebug("No app update available");
            return;
        }

        logger.LogInformation(
            "App update available: VersionCode={VersionCode}, Priority={Priority}",
            updateInfo.AvailableVersionCode,
            updateInfo.UpdatePriority);

        // High priority (4-5): Immediate update (blocking)
        // Normal priority (0-3): Flexible update (background)
        if (updateInfo.UpdatePriority >= updateService.Options.ImmediateUpdatePriority
            && updateInfo.IsImmediateUpdateAllowed)
        {
            logger.LogInformation("Starting immediate update (high priority)");
            await updateService.StartImmediateUpdateAsync();
        }
        else if (updateInfo.IsFlexibleUpdateAllowed)
        {
            logger.LogInformation("Starting flexible update (background)");

            // Subscribe to download completion to prompt restart
            updateService.UpdateDownloaded += OnUpdateDownloaded;

            await updateService.StartFlexibleUpdateAsync();
        }
    }

    private async void OnUpdateDownloaded(object? sender, EventArgs e)
    {
        logger.LogInformation("Update downloaded, completing installation");
        updateService.UpdateDownloaded -= OnUpdateDownloaded;
        await updateService.CompleteUpdateAsync();
    }
}
