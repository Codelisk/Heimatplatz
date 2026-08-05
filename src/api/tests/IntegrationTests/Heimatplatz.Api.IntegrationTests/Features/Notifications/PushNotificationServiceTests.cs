using FluentAssertions;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Heimatplatz.Api.Features.Notifications.Contracts;
using Heimatplatz.Api.Features.Notifications.Data.Entities;
using Heimatplatz.Api.Features.Notifications.Services;
using Heimatplatz.Api.Features.Properties.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;
using Shiny.Extensions.Push;

namespace Heimatplatz.Api.IntegrationTests.Features.Notifications;

[TestFixture]
public class PushNotificationServiceTests
{
    [Test]
    public async Task SendPropertyNotification_IncludesDeepLinkDataForFcmAndApns()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"notification-service-{Guid.NewGuid():N}")
            .Options;
        await using var dbContext = new AppDbContext(options);

        var userId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        dbContext.Set<NotificationPreference>().Add(new NotificationPreference
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FilterMode = NotificationFilterMode.All,
            IsEnabled = true
        });
        dbContext.Set<PushSubscription>().Add(new PushSubscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DeviceToken = "device-token",
            Platform = "iOS"
        });
        await dbContext.SaveChangesAsync();

        Shiny.Extensions.Push.PushNotification? capturedNotification = null;
        IEnumerable<string>? capturedTokens = null;
        var pushManager = Substitute.For<IPushManager>();
        pushManager.SendToTokens(
                Arg.Do<IEnumerable<string>>(tokens => capturedTokens = tokens.ToArray()),
                Arg.Do<Shiny.Extensions.Push.PushNotification>(notification => capturedNotification = notification),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(PushSendResult.Empty(Guid.NewGuid())));

        var service = new PushNotificationService(
            dbContext,
            NullLogger<PushNotificationService>.Instance,
            pushManager);

        await service.SendPropertyNotificationAsync(
            propertyId,
            "Haus am See",
            "Linz",
            500_000m,
            PropertyType.House,
            SellerType.Private);

        capturedTokens.Should().Equal("device-token");
        capturedNotification.Should().NotBeNull();
        capturedNotification!.DeepLink.Should().Be("SHINY_PUSH_NOTIFICATION_CLICK");
        capturedNotification.Data["deepLink"].Should().Be($"heimatplatz://property/{propertyId}");
        capturedNotification.Data["deeplink"].Should().Be($"heimatplatz://property/{propertyId}");
        capturedNotification.Apple!.Category.Should().Be("openProperty");
        capturedNotification.Apple.ThreadId.Should().Be(propertyId.ToString());
    }

    // === Neubauprojekt-Opt-out ===

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"notification-service-{Guid.NewGuid():N}")
            .Options;
        return new AppDbContext(options);
    }

    private static (PushNotificationService Service, IPushManager PushManager) CreateService(AppDbContext dbContext)
    {
        var pushManager = Substitute.For<IPushManager>();
        pushManager.SendToTokens(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<Shiny.Extensions.Push.PushNotification>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(PushSendResult.Empty(Guid.NewGuid())));

        var service = new PushNotificationService(
            dbContext, NullLogger<PushNotificationService>.Instance, pushManager);
        return (service, pushManager);
    }

    private static void AddUserWithSubscription(
        AppDbContext dbContext, Guid userId, NotificationFilterMode mode,
        bool notificationNeubauSelected = true)
    {
        dbContext.Set<NotificationPreference>().Add(new NotificationPreference
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FilterMode = mode,
            IsEnabled = true,
            IsNeubauprojektSelected = notificationNeubauSelected
        });
        dbContext.Set<PushSubscription>().Add(new PushSubscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DeviceToken = $"token-{userId:N}",
            Platform = "Android"
        });
    }

    [Test]
    public async Task SendPropertyNotification_SameAsSearchMitNeubauOptOut_UeberspringtNeubauprojekt()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        AddUserWithSubscription(dbContext, userId, NotificationFilterMode.SameAsSearch);
        dbContext.Set<UserFilterPreferences>().Add(new UserFilterPreferences
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IsNeubauprojektSelected = false
        });
        await dbContext.SaveChangesAsync();

        var (service, pushManager) = CreateService(dbContext);

        // Neubauprojekt: kein Push
        await service.SendPropertyNotificationAsync(
            Guid.NewGuid(), "Neubau-Reihenhaus", "Wels", 289_000m,
            PropertyType.House, SellerType.Private, isNewBuildProject: true);

        await pushManager.DidNotReceive().SendToTokens(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<Shiny.Extensions.Push.PushNotification>(),
            Arg.Any<CancellationToken>());

        // Bestandshaus: Push kommt weiterhin
        await service.SendPropertyNotificationAsync(
            Guid.NewGuid(), "Bestandshaus", "Wels", 289_000m,
            PropertyType.House, SellerType.Private, isNewBuildProject: false);

        await pushManager.Received(1).SendToTokens(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<Shiny.Extensions.Push.PushNotification>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SendPropertyNotification_CustomMitNeubauOptOut_UeberspringtNeubauprojekt()
    {
        await using var dbContext = CreateDbContext();
        AddUserWithSubscription(dbContext, Guid.NewGuid(), NotificationFilterMode.Custom,
            notificationNeubauSelected: false);
        await dbContext.SaveChangesAsync();

        var (service, pushManager) = CreateService(dbContext);

        await service.SendPropertyNotificationAsync(
            Guid.NewGuid(), "Neubau-Reihenhaus", "Wels", 289_000m,
            PropertyType.House, SellerType.Private, isNewBuildProject: true);

        await pushManager.DidNotReceive().SendToTokens(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<Shiny.Extensions.Push.PushNotification>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SendPropertyNotification_AlleModusPushtNeubauprojekteTrotzdem()
    {
        // "Alle neuen Objekte" ignoriert die Suchfilter bewusst komplett (wie bei ZV)
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        AddUserWithSubscription(dbContext, userId, NotificationFilterMode.All);
        dbContext.Set<UserFilterPreferences>().Add(new UserFilterPreferences
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IsNeubauprojektSelected = false
        });
        await dbContext.SaveChangesAsync();

        var (service, pushManager) = CreateService(dbContext);

        await service.SendPropertyNotificationAsync(
            Guid.NewGuid(), "Neubau-Reihenhaus", "Wels", 289_000m,
            PropertyType.House, SellerType.Private, isNewBuildProject: true);

        await pushManager.Received(1).SendToTokens(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<Shiny.Extensions.Push.PushNotification>(),
            Arg.Any<CancellationToken>());
    }
}
