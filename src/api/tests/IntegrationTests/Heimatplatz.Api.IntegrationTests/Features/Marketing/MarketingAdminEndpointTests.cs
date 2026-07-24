using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Heimatplatz.Api.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace Heimatplatz.Api.IntegrationTests.Features.Marketing;

/// <summary>
/// End-to-End-Regressionstests fuer die Marketing-Admin-Endpoints:
/// - komplett leere Kontakte werden abgelehnt (WEB-005)
/// - der Mock-KI-Entwurf kann nicht als echte Mail versendet werden (WEB-006)
/// </summary>
[TestFixture]
[Category(TestCategories.Endpoint)]
[Category(TestCategories.Integration)]
public class MarketingAdminEndpointTests : BaseApiIntegrationTest
{
    private const string AdminKey = "integration-test-admin-key";

    protected override WebApplicationFactory<Program> CreateFactory()
        => new SqliteWebApplicationFactory<Program>(new Dictionary<string, string>
        {
            ["Admin:ApiKey"] = AdminKey
        });

    protected override void OnSetUp()
    {
        Client.DefaultRequestHeaders.Add("X-Admin-Key", AdminKey);
    }

    [Test]
    public async Task SaveEmptyMarketingContact_IsRejected()
    {
        var response = await Client.PostAsJsonAsync("/api/admin/marketing/contacts/save", new
        {
            Email = "",
            Name = "",
            Company = "",
            Phone = "",
            City = "",
            Notes = ""
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("Success").GetBoolean().Should().BeFalse();
        json.RootElement.GetProperty("Error").GetString().Should().Contain("Mindestens");
    }

    [Test]
    public async Task SaveContactWithOnlyCompany_Succeeds()
    {
        // Firmenpool-Uebernahmen haben zunaechst nur Firma und Ort - das muss weiter gehen
        var response = await Client.PostAsJsonAsync("/api/admin/marketing/contacts/save", new
        {
            Email = "",
            Name = "",
            Company = "Testfirma Immobilien GmbH",
            City = "Linz"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("Success").GetBoolean().Should().BeTrue();
    }

    [Test]
    public async Task GeneratedMockDraft_CannotBeSent()
    {
        // In der Testumgebung ist kein KI-Anbieter konfiguriert -> Mock-Generator
        var generateResponse = await Client.PostAsJsonAsync("/api/admin/marketing/email/generate", new
        {
            Keywords = "Makler, Oberoesterreich, Onboarding",
            RecipientName = "Max Mustermann"
        });
        generateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var generateJson = JsonDocument.Parse(await generateResponse.Content.ReadAsStringAsync());
        generateJson.RootElement.GetProperty("Success").GetBoolean().Should().BeTrue();
        var mockBody = generateJson.RootElement.GetProperty("Body").GetString();
        mockBody.Should().NotBeNullOrEmpty();
        mockBody.Should().Contain("[PLATZHALTER OHNE KI]");

        // Der unveraenderte Mock-Entwurf darf nicht rausgehen (WEB-006)
        var sendResponse = await Client.PostAsJsonAsync("/api/admin/marketing/email/send", new
        {
            RecipientEmail = "kunde@example.com",
            Subject = "Heimatplatz: Ihre Anfrage",
            Body = mockBody
        });
        sendResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var sendJson = JsonDocument.Parse(await sendResponse.Content.ReadAsStringAsync());
        sendJson.RootElement.GetProperty("Success").GetBoolean().Should().BeFalse();
        sendJson.RootElement.GetProperty("Error").GetString().Should().Contain("Platzhalter");
    }
}
