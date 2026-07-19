using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Heimatplatz.Api.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace Heimatplatz.Api.IntegrationTests.Endpoints;

/// <summary>
/// Positiv-Pfad der /api/admin-Endpoints (Intern-Bereich): mit konfiguriertem
/// Admin:ApiKey und passendem X-Admin-Key-Header sind die Endpoints erreichbar,
/// mit falschem Key bleiben sie gesperrt. Der Fail-Closed-Fall ohne konfigurierten
/// Key liegt in <see cref="SecurityRegressionTests"/>.
/// </summary>
[TestFixture]
[Category(TestCategories.Endpoint)]
[Category(TestCategories.Integration)]
public class AdminEndpointTests : BaseApiIntegrationTest
{
    private const string TestAdminKey = "integration-test-admin-key";

    protected override WebApplicationFactory<Program> CreateFactory()
        => new AdminKeyWebApplicationFactory();

    protected override void OnSetUp()
        => Client.DefaultRequestHeaders.Add("X-Admin-Key", TestAdminKey);

    [Test]
    public async Task AdminStats_WithAdminKey_ReturnsOk()
    {
        var response = await Client.GetAsync("/api/admin/stats");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task AdminUsers_WithAdminKey_ReturnsOk()
    {
        var response = await Client.GetAsync("/api/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task AdminProperties_WithAdminKey_ReturnsOk()
    {
        var response = await Client.GetAsync("/api/admin/properties?source=foreclosure&hidden=true");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task AdminSetPropertyVisibility_UnknownId_ReturnsNotFound()
    {
        var response = await Client.PostAsJsonAsync("/api/admin/properties/visibility", new
        {
            Id = Guid.NewGuid(),
            Hidden = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task AdminStats_WithWrongAdminKey_ReturnsUnauthorized()
    {
        Client.DefaultRequestHeaders.Remove("X-Admin-Key");
        Client.DefaultRequestHeaders.Add("X-Admin-Key", "wrong-key");

        var response = await Client.GetAsync("/api/admin/stats");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed class AdminKeyWebApplicationFactory : CustomWebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseSetting("Admin:ApiKey", TestAdminKey);
        }
    }
}
