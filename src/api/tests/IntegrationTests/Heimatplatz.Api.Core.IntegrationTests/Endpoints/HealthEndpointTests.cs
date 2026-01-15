using Heimatplatz.Api.IntegrationTests.Infrastructure;
using FluentAssertions;
using NUnit.Framework;
using System.Net;

namespace Heimatplatz.Api.Core.IntegrationTests.Endpoints;

/// <summary>
/// Tests fuer Health-Endpoints.
/// </summary>
[TestFixture]
[Category(TestCategories.Core)]
[Category(TestCategories.Endpoint)]
[Category(TestCategories.Integration)]
public class HealthEndpointTests : BaseApiIntegrationTest
{
    [Test]
    [Category(TestCategories.Smoke)]
    public async Task HealthEndpoint_ReturnsOk()
    {
        // Arrange & Act
        var response = await Client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
