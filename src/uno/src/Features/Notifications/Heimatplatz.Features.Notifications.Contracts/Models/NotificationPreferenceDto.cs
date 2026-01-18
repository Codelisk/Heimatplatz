namespace Heimatplatz.Features.Notifications.Contracts.Models;

/// <summary>
/// Data transfer object for notification preferences
/// </summary>
/// <param name="IsEnabled">Whether notifications are enabled</param>
/// <param name="Locations">List of locations user wants notifications for</param>
public record NotificationPreferenceDto(
    bool IsEnabled,
    List<string> Locations
);
