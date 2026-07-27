using System.Text.Json;
using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Core.Data.Seeding;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Heimatplatz.Api.Features.Notifications.Contracts;
using Heimatplatz.Api.Features.Notifications.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shiny;

namespace Heimatplatz.Api.Features.Notifications.Data.Seeding;

/// <summary>
/// Seeder for notification preferences test data.
/// Push subscriptions are never seeded because provider tokens must come from real devices.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class NotificationsSeeder(AppDbContext dbContext) : ISeeder
{
    public int Order => 30; // Run after users and properties

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Older test data contained synthetic provider tokens. They can never be delivered by
        // FCM/APNs and would make every broadcast look partially broken. Keep this cleanup before
        // the preference early-return so existing test databases are repaired on the next start.
        await dbContext.Set<PushSubscription>()
            .Where(x => x.DeviceToken.StartsWith("test-device-token-"))
            .ExecuteDeleteAsync(cancellationToken);

        // Only seed if no notification preferences exist
        if (await dbContext.Set<NotificationPreference>().AnyAsync(cancellationToken))
            return;

        // Get users for seeding
        var users = await dbContext.Set<User>().ToListAsync(cancellationToken);
        if (!users.Any())
            return;

        var preferences = new List<NotificationPreference>();
        // Cities to use for preferences (matching PropertySeeder)
        var cities = new[] { "Linz", "Wels", "Gmunden", "Bad Ischl", "Steyr", "Leonding", "Freistadt", "Traun" };

        // Create notification preferences for users - one preference per user.
        // "All" bewusst nicht seeden: der Modus benachrichtigt ueber jedes neue
        // Objekt und wuerde auf Testkonten wie eine ZV-Vorauswahl wirken.
        var filterModes = new[] { NotificationFilterMode.SameAsSearch, NotificationFilterMode.Custom };

        foreach (var (user, index) in users.Take(5).Select((u, i) => (u, i)))
        {
            var filterMode = filterModes[index % filterModes.Length];
            var userCities = cities.OrderBy(_ => Guid.NewGuid()).Take(Random.Shared.Next(1, 4)).ToList();

            preferences.Add(new NotificationPreference
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                FilterMode = filterMode,
                IsEnabled = true,
                SelectedLocationsJson = JsonSerializer.Serialize(userCities),
                IsHausSelected = true,
                IsGrundstueckSelected = true,
                // Zwangsversteigerungen sind - wie in der Suche - nie vorausgewaehlt;
                // geseedete Testkonten sahen sonst ein ZV-Abo, das niemand gewaehlt hat
                IsZwangsversteigerungSelected = false,
                IsPrivateSelected = true,
                IsBrokerSelected = true,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-Random.Shared.Next(1, 30))
            });
        }

        dbContext.Set<NotificationPreference>().AddRange(preferences);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
