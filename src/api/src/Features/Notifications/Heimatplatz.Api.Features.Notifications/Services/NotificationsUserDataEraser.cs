using Heimatplatz.Api.Cleanup;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Notifications.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Heimatplatz.Api.Features.Notifications.Services;

/// <summary>
/// Loescht alle Benachrichtigungs-Daten eines Benutzers im Rahmen der Konto-Loeschung:
/// Push-Subscriptions (Geraete-Tokens) und Notification-Einstellungen.
/// Registrierung erfolgt in <c>AddNotificationsFeature</c>.
/// </summary>
public class NotificationsUserDataEraser(AppDbContext dbContext) : IUserDataEraser
{
    /// <summary>Keine FK-Abhaengigkeiten zu anderen Features -> frueh ausfuehrbar.</summary>
    public int Order => 10;

    public async Task EraseUserDataAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await dbContext.Set<PushSubscription>()
            .Where(s => s.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.Set<NotificationPreference>()
            .Where(p => p.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
