using FluentAssertions;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Notifications.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shiny.Extensions.Push;

namespace Heimatplatz.Api.IntegrationTests.Features.Notifications;

[TestFixture]
public class EfPushRepositoryTests
{
    private ServiceProvider serviceProvider = null!;
    private EfPushRepository repository = null!;
    private readonly Guid userId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();
        var databaseRoot = new InMemoryDatabaseRoot();
        var databaseName = $"push-tests-{Guid.NewGuid():N}";
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(databaseName, databaseRoot));
        services.AddSingleton<EfPushRepository>();

        serviceProvider = services.BuildServiceProvider();
        repository = serviceProvider.GetRequiredService<EfPushRepository>();
    }

    [TearDown]
    public async Task TearDown()
    {
        await serviceProvider.DisposeAsync();
    }

    [Test]
    public async Task Save_WithStableDeviceId_RotatesTokenWithoutCreatingDuplicate()
    {
        await repository.Save(CreateRegistration("old-token", PushEnvironment.Sandbox));
        await repository.Save(CreateRegistration("new-token", PushEnvironment.Sandbox));

        var registrations = await repository.GetRegistrations(PushFilter.Broadcast);

        registrations.Should().ContainSingle();
        registrations.Single().DeviceToken.Should().Be("new-token");
        registrations.Single().Environment.Should().Be(PushEnvironment.Sandbox);
    }

    [Test]
    public async Task StreamRegistrations_AppliesEnvironmentAndPlatformFilters()
    {
        await repository.Save(CreateRegistration("sandbox-ios", PushEnvironment.Sandbox));
        await repository.Save(new DeviceRegistration
        {
            DeviceToken = "production-android",
            DeviceId = "android-installation",
            UserIdentifier = userId.ToString(),
            Platform = DevicePlatform.Android,
            Environment = PushEnvironment.Production
        });

        var registrations = await repository.GetRegistrations(new PushFilter
        {
            Environment = PushEnvironment.Sandbox,
            Platforms = [DevicePlatform.iOS]
        });

        registrations.Select(x => x.DeviceToken).Should().Equal("sandbox-ios");
    }

    [Test]
    public async Task SubscribeAndRemove_PersistTopicAndDeleteRegistration()
    {
        await repository.Save(CreateRegistration("topic-token", PushEnvironment.Production));
        await repository.Subscribe("topic-token", DevicePlatform.iOS, "properties");

        var topicRegistrations = await repository.GetRegistrations(new PushFilter { Topic = "properties" });
        topicRegistrations.Should().ContainSingle();

        var removed = await repository.Remove("topic-token", DevicePlatform.iOS);
        removed.Should().BeTrue();
        (await repository.GetRegistrations(PushFilter.Broadcast)).Should().BeEmpty();
    }

    private DeviceRegistration CreateRegistration(string token, PushEnvironment environment) => new()
    {
        DeviceToken = token,
        DeviceId = "ios-installation",
        UserIdentifier = userId.ToString(),
        Platform = DevicePlatform.iOS,
        Environment = environment
    };
}
