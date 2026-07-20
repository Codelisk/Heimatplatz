using FluentAssertions;
using Heimatplatz.Api.Features.Notifications.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shiny.Extensions.Push;

namespace Heimatplatz.Api.IntegrationTests.Features.Notifications;

[TestFixture]
public class PushProvidersConfigurationTests
{
    [Test]
    public void AddPushProviders_DisablesProviderBatching()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddPushProviders(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<PushManagerOptions>>().Value;

        options.EnableBatching.Should().BeFalse();
    }
}
