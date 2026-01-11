using Heimatplatz.Core.Startup;
using Heimatplatz.UnitTests.Infrastructure;
using FluentAssertions;
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

    protected override void OnSetUp()
    {
        _services = new ServiceCollection();
    }

    [Test]
    [Category(TestCategories.Smoke)]
    public void AddAppServices_RegistersServices()
    {
        // Act
        _services.AddAppServices();

        // Assert
        _services.Should().NotBeEmpty("weil AddAppServices Services registrieren sollte");
    }

    [Test]
    public void AddAppServices_CanBuildServiceProvider()
    {
        // Act
        _services.AddAppServices();
        var provider = _services.BuildServiceProvider();

        // Assert
        provider.Should().NotBeNull();
    }
}
