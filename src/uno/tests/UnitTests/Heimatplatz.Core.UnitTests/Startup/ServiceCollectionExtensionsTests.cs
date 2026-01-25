using Heimatplatz.Core.Startup;
using Heimatplatz.UnitTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Heimatplatz.Core.UnitTests.Startup;

/// <summary>
/// Tests fuer ServiceCollectionExtensions.
/// </summary>
[TestFixture]
[Category(TestCategories.Core)]
[Category(TestCategories.Unit)]
public class ServiceCollectionExtensionsTests : BaseUnitTest
{
    private IServiceCollection _services = null!;
    private IConfiguration _configuration = null!;

    protected override void OnSetUp()
    {
        _services = new ServiceCollection();
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mediator:Http:Heimatplatz.Core.ApiClient.Generated.*"] = "http://localhost:5292"
            })
            .Build();
    }

    [Test]
    [Category(TestCategories.Smoke)]
    public void AddAppServices_RegistersServices()
    {
        // Act
        _services.AddAppServices(_configuration);

        // Assert
        _services.Should().NotBeEmpty("weil AddAppServices Services registrieren sollte");
    }

    [Test]
    public void AddAppServices_CanBuildServiceProvider()
    {
        // Act
        _services.AddAppServices(_configuration);
        var provider = _services.BuildServiceProvider();

        // Assert
        provider.Should().NotBeNull();
    }
}
