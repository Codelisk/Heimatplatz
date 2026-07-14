using System.Runtime.CompilerServices;
using System.Text.Json;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Notifications.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Extensions.Push;

namespace Heimatplatz.Api.Features.Notifications.Services;

/// <summary>
/// Persists Shiny push registrations in the existing PushSubscriptions table.
/// The Shiny push manager is a singleton, so every operation creates its own DI scope/DbContext.
/// </summary>
public sealed class EfPushRepository(IServiceScopeFactory scopeFactory) : IPushRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task Save(DeviceRegistration registration, CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var subscriptions = dbContext.Set<PushSubscription>();

        PushSubscription? entity = null;
        if (!string.IsNullOrWhiteSpace(registration.DeviceId))
        {
            entity = await subscriptions.FirstOrDefaultAsync(
                x => x.DeviceId == registration.DeviceId,
                cancellationToken);
        }

        var tokenEntity = await subscriptions.FirstOrDefaultAsync(
            x => x.DeviceToken == registration.DeviceToken,
            cancellationToken);

        if (entity is null)
        {
            entity = tokenEntity;
        }
        else if (tokenEntity is not null && tokenEntity.Id != entity.Id)
        {
            // A token may already have been stored by a legacy registration without DeviceId.
            // Consolidate both rows before assigning the unique token to the stable installation.
            subscriptions.Remove(tokenEntity);
        }

        var userId = ParseUserId(registration.UserIdentifier)
            ?? entity?.UserId
            ?? throw new InvalidOperationException("A Heimatplatz push registration requires a valid user identifier.");

        if (entity is null)
        {
            entity = new PushSubscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DeviceToken = registration.DeviceToken,
                Platform = ToStoredPlatform(registration.Platform),
                CreatedAt = DateTimeOffset.UtcNow
            };
            subscriptions.Add(entity);
        }

        entity.UserId = userId;
        entity.DeviceToken = registration.DeviceToken;
        entity.Platform = ToStoredPlatform(registration.Platform);
        entity.DeviceId = registration.DeviceId;
        entity.AppId = registration.AppId;
        entity.Environment = registration.Environment;
        entity.TagsJson = JsonSerializer.Serialize(registration.Tags, JsonOptions);
        entity.TopicsJson = JsonSerializer.Serialize(registration.Topics, JsonOptions);
        entity.Locale = registration.Locale;
        entity.AppVersion = registration.AppVersion;
        entity.ExpiresAt = registration.ExpiresAt;
        entity.DataJson = registration.Data is null
            ? null
            : JsonSerializer.Serialize(registration.Data, JsonOptions);
        entity.SubscribedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> Remove(
        string deviceToken,
        DevicePlatform platform,
        CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var platformAliases = StoredPlatformAliases(platform);
        var entities = await dbContext.Set<PushSubscription>()
            .Where(x => x.DeviceToken == deviceToken && platformAliases.Contains(x.Platform))
            .ToListAsync(cancellationToken);
        if (entities.Count == 0)
            return false;

        dbContext.RemoveRange(entities);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task UpdateToken(
        string oldToken,
        DevicePlatform platform,
        string newToken,
        CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var platformAliases = StoredPlatformAliases(platform);
        var subscriptions = dbContext.Set<PushSubscription>();

        var entity = await subscriptions.FirstOrDefaultAsync(
            x => x.DeviceToken == oldToken && platformAliases.Contains(x.Platform),
            cancellationToken);
        if (entity is null)
            return;

        var duplicate = await subscriptions.FirstOrDefaultAsync(
            x => x.DeviceToken == newToken && x.Id != entity.Id,
            cancellationToken);
        if (duplicate is not null)
            subscriptions.Remove(duplicate);

        entity.DeviceToken = newToken;
        entity.SubscribedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task Subscribe(
        string deviceToken,
        DevicePlatform platform,
        string topic,
        CancellationToken cancellationToken = default)
        => UpdateTopics(deviceToken, platform, topic, subscribe: true, cancellationToken);

    public Task Unsubscribe(
        string deviceToken,
        DevicePlatform platform,
        string topic,
        CancellationToken cancellationToken = default)
        => UpdateTopics(deviceToken, platform, topic, subscribe: false, cancellationToken);

    public async Task<IReadOnlyList<DeviceRegistration>> GetRegistrations(
        PushFilter filter,
        CancellationToken cancellationToken = default)
    {
        var registrations = new List<DeviceRegistration>();
        await foreach (var registration in StreamRegistrations(filter, cancellationToken))
            registrations.Add(registration);
        return registrations;
    }

    public async IAsyncEnumerable<DeviceRegistration> StreamRegistrations(
        PushFilter filter,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        IQueryable<PushSubscription> query = dbContext.Set<PushSubscription>().AsNoTracking();

        if (filter.UserIdentifier is not null)
        {
            var userId = ParseUserId(filter.UserIdentifier);
            if (userId is null)
                yield break;
            query = query.Where(x => x.UserId == userId.Value);
        }

        if (filter.Environment is not null)
            query = query.Where(x => x.Environment == filter.Environment.Value);

        if (filter.AppId is not null)
            query = query.Where(x => x.AppId == filter.AppId);

        if (filter.DeviceTokens is { Count: > 0 })
        {
            var deviceTokens = filter.DeviceTokens.ToArray();
            query = query.Where(x => deviceTokens.Contains(x.DeviceToken));
        }

        var platforms = filter.Platforms?.Select(ToStoredPlatform).ToArray();
        if (platforms is { Length: > 0 })
            query = query.Where(x => platforms.Contains(x.Platform));

        await foreach (var entity in query.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            var registration = ToRegistration(entity);
            if (filter.Matches(registration))
                yield return registration;
        }
    }

    private async Task UpdateTopics(
        string deviceToken,
        DevicePlatform platform,
        string topic,
        bool subscribe,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var platformAliases = StoredPlatformAliases(platform);
        var entity = await dbContext.Set<PushSubscription>().FirstOrDefaultAsync(
            x => x.DeviceToken == deviceToken && platformAliases.Contains(x.Platform),
            cancellationToken);
        if (entity is null)
            return;

        var topics = DeserializeList(entity.TopicsJson);
        if (subscribe)
        {
            if (!topics.Contains(topic, StringComparer.Ordinal))
                topics.Add(topic);
        }
        else
        {
            topics.RemoveAll(x => string.Equals(x, topic, StringComparison.Ordinal));
        }

        entity.TopicsJson = JsonSerializer.Serialize(topics, JsonOptions);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static DeviceRegistration ToRegistration(PushSubscription entity) => new()
    {
        DeviceToken = entity.DeviceToken,
        Platform = ToDevicePlatform(entity.Platform),
        DeviceId = entity.DeviceId,
        AppId = entity.AppId,
        UserIdentifier = entity.UserId.ToString(),
        Environment = entity.Environment,
        Tags = DeserializeList(entity.TagsJson),
        Topics = DeserializeList(entity.TopicsJson),
        Locale = entity.Locale,
        AppVersion = entity.AppVersion,
        ExpiresAt = entity.ExpiresAt,
        Data = DeserializeDictionary(entity.DataJson)
    };

    private static Guid? ParseUserId(string? value)
        => Guid.TryParse(value, out var userId) ? userId : null;

    private static string ToStoredPlatform(DevicePlatform platform) => platform switch
    {
        DevicePlatform.iOS => "iOS",
        DevicePlatform.MacOS => "MacCatalyst",
        DevicePlatform.Android => "Android",
        DevicePlatform.Windows => "Windows",
        DevicePlatform.WebBrowser => "Web",
        _ => throw new ArgumentOutOfRangeException(nameof(platform), platform, null)
    };

    private static string[] StoredPlatformAliases(DevicePlatform platform) => platform switch
    {
        DevicePlatform.iOS => ["iOS", "ios"],
        DevicePlatform.MacOS => ["MacCatalyst", "maccatalyst", "MacOS", "macOS", "macos"],
        DevicePlatform.Android => ["Android", "android"],
        DevicePlatform.Windows => ["Windows", "windows", "Desktop", "desktop"],
        DevicePlatform.WebBrowser => ["Web", "web", "WebBrowser", "webbrowser"],
        _ => throw new ArgumentOutOfRangeException(nameof(platform), platform, null)
    };

    private static DevicePlatform ToDevicePlatform(string platform) => platform.ToLowerInvariant() switch
    {
        "ios" => DevicePlatform.iOS,
        "macos" or "maccatalyst" => DevicePlatform.MacOS,
        "android" => DevicePlatform.Android,
        "windows" or "desktop" => DevicePlatform.Windows,
        "web" or "webbrowser" => DevicePlatform.WebBrowser,
        _ => throw new InvalidOperationException($"Unsupported push platform '{platform}'.")
    };

    private static List<string> DeserializeList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static IReadOnlyDictionary<string, string>? DeserializeDictionary(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
