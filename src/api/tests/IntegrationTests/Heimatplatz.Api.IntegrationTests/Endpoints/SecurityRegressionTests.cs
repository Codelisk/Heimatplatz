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
    public async Task AdminUsers_WithoutAdminKey_ReturnsUnauthorized()
    {
        // Testing-Environment + kein konfigurierter Admin:ApiKey = fail-closed (AdminAccessGuard)
        var response = await Client.GetAsync("/api/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task AdminProperties_WithoutAdminKey_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/admin/properties");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task AdminStats_WithoutAdminKey_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/admin/stats");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task AdminSetPropertyVisibility_WithoutAdminKey_ReturnsUnauthorized()
    {
        var response = await Client.PostAsJsonAsync("/api/admin/properties/visibility", new
        {
            Id = Guid.NewGuid(),
            Hidden = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task AdminDeleteProperty_WithoutAdminKey_ReturnsUnauthorized()
    {
        var response = await Client.DeleteAsync($"/api/admin/properties/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task UpdateContactSettings_WithoutAdminKey_ReturnsUnauthorized()
    {
        // Kontakt-Stammdaten sind zur Laufzeit aenderbar - der Schreibweg muss genauso
        // fail-closed sein wie /api/admin/*, sonst kann jeder das Impressum umschreiben
        var response = await Client.PostAsJsonAsync("/api/admin/legal/contact", new
        {
            Phone = "+43 664 0000000"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task UpdateImprintParty_WithoutAdminKey_ReturnsUnauthorized()
    {
        var response = await Client.PostAsJsonAsync("/api/admin/legal/imprint", new
        {
            CompanyName = "Fremd",
            LegalForm = "Einzelunternehmen",
            Owner = "Fremd",
            Street = "Weg 1",
            PostalCode = "4663",
            City = "Laakirchen",
            Country = "Österreich",
            Email = "fremd@example.at",
            UidNumber = "ATU00000000",
            TaxNumber = "000000000",
            Trade = "Handel",
            TradeAuthority = "BH",
            ProfessionalLaw = "GewO 1994"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetContactInfo_IsPubliclyReadable()
    {
        // Gegenstueck: die Leseseite muss ohne Key erreichbar bleiben (Footer, JSON-LD, MAUI)
        var response = await Client.GetAsync("/api/legal/contact");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
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
