using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Core.Data.Seeding;
using Heimatplatz.Api.Features.Auth.Data.Entities;
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

        // Create notification preferences for users
        foreach (var user in users.Take(5)) // First 5 users get preferences
        {
            // Each user gets 1-3 random city preferences
            var userCities = cities.OrderBy(_ => Guid.NewGuid()).Take(Random.Shared.Next(1, 4)).ToList();

            foreach (var city in userCities)
            {
                preferences.Add(new NotificationPreference
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Location = city,
                    IsEnabled = true,
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-Random.Shared.Next(1, 30))
                });
            }

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
