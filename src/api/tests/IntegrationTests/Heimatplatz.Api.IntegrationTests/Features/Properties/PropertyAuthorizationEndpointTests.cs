using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Heimatplatz.Api.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace Heimatplatz.Api.IntegrationTests.Features.Properties;

/// <summary>
/// End-to-End-Regressionstests fuer Eigentums- und Berechtigungspruefungen.
/// </summary>
[TestFixture]
[Category(TestCategories.Endpoint)]
[Category(TestCategories.Integration)]
public class PropertyAuthorizationEndpointTests : BaseApiIntegrationTest
{
    private const string Password = "Passwort123!";

    protected override WebApplicationFactory<Program> CreateFactory()
        => new SqliteWebApplicationFactory<Program>();

    [Test]
    public async Task ReadForEditUpdateAndDeleteForeignProperty_ReturnForbidden()
    {
        var ownerAccessToken = await RegisterSellerAsync("owner");
        var intruderAccessToken = await RegisterSellerAsync("intruder");

        var locationsResponse = await Client.GetAsync("/api/locations");
        locationsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var locationsJson = JsonDocument.Parse(await locationsResponse.Content.ReadAsStringAsync());
        var municipalityId = locationsJson.RootElement
            .GetProperty("FederalProvinces")[0]
            .GetProperty("Districts")[0]
            .GetProperty("Municipalities")[0]
            .GetProperty("Id")
            .GetGuid();

        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ownerAccessToken);
        var createResponse = await Client.PostAsJsonAsync("/api/properties", new
        {
            Title = "Testhaus des Eigentuemers",
            Address = "Teststrasse 1",
            MunicipalityId = municipalityId,
            Price = 350_000,
            Type = "House",
            Description = "Ausfuehrliche Beschreibung des Testhauses mit deutlich mehr als fuenfzig Zeichen.",
            LivingAreaSquareMeters = 120,
            Rooms = 5,
            Features = Array.Empty<string>(),
            ImageUrls = Array.Empty<string>()
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var createJson = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var propertyId = createJson.RootElement.GetProperty("PropertyId").GetGuid();

        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", intruderAccessToken);
        var foreignEditResponse = await Client.GetAsync($"/api/properties/{propertyId}/edit");

        foreignEditResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ownerAccessToken);
        var ownerEditResponse = await Client.GetAsync($"/api/properties/{propertyId}/edit");

        ownerEditResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var ownerEditJson = JsonDocument.Parse(await ownerEditResponse.Content.ReadAsStringAsync());
        ownerEditJson.RootElement
            .GetProperty("Property")
            .GetProperty("Id")
            .GetGuid()
            .Should()
            .Be(propertyId);

        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", intruderAccessToken);
        var updateResponse = await Client.PutAsJsonAsync("/api/properties", new
        {
            Id = propertyId,
            Title = "Unzulaessige Aenderung",
            Address = "Andere Teststrasse 2",
            MunicipalityId = municipalityId,
            Price = 400_000,
            Type = "House",
            Description = "Auch diese Beschreibung ist lang genug, darf aber niemals gespeichert werden.",
            LivingAreaSquareMeters = 130,
            Rooms = 6,
            Features = Array.Empty<string>(),
            ImageUrls = Array.Empty<string>()
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var deleteResponse = await Client.DeleteAsync($"/api/properties/{propertyId}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<string> RegisterSellerAsync(string prefix)
    {
        var response = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            FirstName = "Max",
            LastName = "Mustermann",
            Email = $"{prefix}-{Guid.NewGuid():N}@heimatplatz.dev",
            Password,
            SellerType = "Private"
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("AccessToken").GetString()
            ?? throw new InvalidOperationException("Registrierung lieferte kein Access Token.");
    }
}
