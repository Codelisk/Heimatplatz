using Heimatplatz.Api.UnitTests.Infrastructure;
using FluentAssertions;
using NUnit.Framework;

namespace Heimatplatz.Api.Core.UnitTests.Data;

/// <summary>
/// Tests fuer AppDbContext.
/// </summary>
[TestFixture]
[Category(TestCategories.Core)]
[Category(TestCategories.Data)]
[Category(TestCategories.Unit)]
public class AppDbContextTests : BaseApiUnitTest
{
    [Test]
    [Category(TestCategories.Smoke)]
    public void AppDbContext_CanBeCreated()
    {
        // Arrange & Act & Assert
        // TODO: Implementiere mit InMemory-DbContext
        true.Should().BeTrue("Placeholder-Test");
    }
}
