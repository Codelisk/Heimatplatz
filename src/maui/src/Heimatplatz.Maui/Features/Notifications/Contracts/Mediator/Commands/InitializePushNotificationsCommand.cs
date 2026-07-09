using Shiny.Mediator;

namespace Heimatplatz.Features.Notifications.Contracts.Mediator.Commands;

/// <summary>
/// Command zum Initialisieren der Push Notifications nach Login oder Registrierung.
/// Fordert die Berechtigung an und registriert das Geraet beim API.
/// Plattformunterschiede werden vom Handler transparent behandelt.
/// </summary>
public record InitializePushNotificationsCommand : ICommand;
