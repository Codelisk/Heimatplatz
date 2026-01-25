using Heimatplatz.Core.Startup;
using Heimatplatz.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Heimatplatz.Core.IntegrationTests.Startup;

/// <summary>
/// Tests fuer DI-Container-Konfiguration.
/// </summary>
[TestFixture]
[Category(TestCategories.Core)]
[Category(TestCategories.Integration)]
public class DependencyInjectionTests : BaseIntegrationTest
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        services.AddAppServices(Configuration);
    }

    [Test]
    [Category(TestCategories.Smoke)]
    public void AllServices_CanBeResolved()
    {
        // Assert
        ServiceProvider.Should().NotBeNull();
    }

    [Test]
    public void ServiceProvider_IsConfigured()
    {
        // Assert
        Services.Should().NotBeEmpty("weil Services registriert sein sollten");
    }
}
