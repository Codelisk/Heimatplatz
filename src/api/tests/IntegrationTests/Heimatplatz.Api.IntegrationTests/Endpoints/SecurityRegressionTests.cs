using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Heimatplatz.Api.IntegrationTests.Infrastructure;
using NUnit.Framework;

namespace Heimatplatz.Api.IntegrationTests.Endpoints;

/// <summary>
/// Regressionstests fuer die Absicherung administrativer Endpoints (Juli 2026):
/// /api/db/init und /api/locations/seed wurden entfernt (anonym erreichbar,
/// db/init konnte die Datenbank loeschen), test-push verlangt RequireAdmin,
/// der Edikte-Sync-Trigger einen Shared-Key (X-Sync-Key, fail-closed).
/// </summary>
[TestFixture]
[Category(TestCategories.Endpoint)]
[Category(TestCategories.Integration)]
public class SecurityRegressionTests : BaseApiIntegrationTest
{
    [Test]
    [Category(TestCategories.Smoke)]
    public async Task DbInit_Endpoint_IsRemoved()
    {
        var response = await Client.PostAsJsonAsync("/api/db/init", new { });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task LocationsSeed_Endpoint_IsRemoved()
    {
        var response = await Client.PostAsJsonAsync("/api/locations/seed", new { });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task TestPush_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await Client.PostAsJsonAsync("/api/notifications/test-push", new
        {
            Title = "Test",
            Body = "Test"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ForeclosureSync_WithoutTriggerKey_ReturnsUnauthorized()
    {
        // Testing-Environment + kein konfigurierter SyncTriggerKey = fail-closed
        var response = await Client.PostAsJsonAsync("/api/foreclosure-auctions/sync", new { });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task CreateProperty_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await Client.PostAsJsonAsync("/api/properties/", new
        {
            Title = "Nicht erlaubt"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetProperties_IsPubliclyReadable()
    {
        var response = await Client.GetAsync("/api/properties/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
