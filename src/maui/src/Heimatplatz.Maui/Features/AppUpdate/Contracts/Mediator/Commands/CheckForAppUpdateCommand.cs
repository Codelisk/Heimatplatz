using Shiny.Mediator;

namespace Heimatplatz.Features.AppUpdate.Contracts.Mediator.Commands;

/// <summary>
/// Command to check for available app updates and start the update flow if available.
/// On Android, this triggers the Google Play In-App Update flow.
/// On other platforms, this is a no-op.
/// </summary>
public record CheckForAppUpdateCommand : ICommand;
