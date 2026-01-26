using System.Text.Json;
using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Core.Data.Seeding;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Heimatplatz.Api.Features.Notifications.Contracts;
using Heimatplatz.Api.Features.Notifications.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shiny.Extensions.DependencyInjection;

namespace Heimatplatz.Api.Features.Notifications.Data.Seeding;

/// <summary>
/// Seeder for notification preferences and push subscriptions test data
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class NotificationsSeeder(AppDbContext dbContext) : ISeeder
{
    public int Order => 30; // Run after users and properties

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Only seed if no notification preferences exist
        if (await dbContext.Set<NotificationPreference>().AnyAsync(cancellationToken))
            return;

        // Get users for seeding
        var users = await dbContext.Set<User>().ToListAsync(cancellationToken);
        if (!users.Any())
            return;

        var preferences = new List<NotificationPreference>();
        var subscriptions = new List<PushSubscription>();

        // Cities to use for preferences (matching PropertySeeder)
        var cities = new[] { "Linz", "Wels", "Gmunden", "Bad Ischl", "Steyr", "Leonding", "Freistadt", "Traun" };

        // Create notification preferences for users - one preference per user
        var filterModes = new[] { NotificationFilterMode.All, NotificationFilterMode.SameAsSearch, NotificationFilterMode.Custom };

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
                IsZwangsversteigerungSelected = index % 2 == 0,
                IsPrivateSelected = true,
                IsBrokerSelected = true,
                IsPortalSelected = index % 3 != 0,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-Random.Shared.Next(1, 30))
            });

            // Create push subscription for this user
            subscriptions.Add(new PushSubscription
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                DeviceToken = $"test-device-token-{user.Id:N}",
                Platform = Random.Shared.Next(0, 3) switch
                {
                    0 => "Desktop",
                    1 => "iOS",
                    _ => "Android"
                },
                SubscribedAt = DateTimeOffset.UtcNow.AddDays(-Random.Shared.Next(1, 15)),
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-Random.Shared.Next(1, 15))
            });
        }

        dbContext.Set<NotificationPreference>().AddRange(preferences);
        dbContext.Set<PushSubscription>().AddRange(subscriptions);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
