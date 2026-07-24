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
/// End-to-End-Regressionstests fuer die Listen-/Detaildaten:
/// - eingegebene PLZ bleibt erhalten (WEB-009, Property.PostalCode)
/// - Zwangsversteigerungen liefern den Auktionstermin im Listen-DTO (WEB-011)
///   und Price == MinimumBid (WEB-007/014)
/// </summary>
[TestFixture]
[Category(TestCategories.Endpoint)]
[Category(TestCategories.Integration)]
public class PropertyListDataEndpointTests : BaseApiIntegrationTest
{
    private const string Password = "Passwort123!";

    protected override WebApplicationFactory<Program> CreateFactory()
        => new SqliteWebApplicationFactory<Program>();

    [Test]
    public async Task CreatePropertyWithCustomPostalCode_KeepsEnteredPostalCode()
    {
        var accessToken = await RegisterSellerAsync("plz");
        var (municipalityId, municipalityPostalCode) = await GetFirstMunicipalityAsync();

        // Abweichende, aber plausible PLZ (z.B. 4040 Linz statt 4020)
        var enteredPostalCode = municipalityPostalCode == "4040" ? "4041" : "4040";

        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
        var createResponse = await Client.PostAsJsonAsync("/api/properties", new
        {
            Title = "Testhaus mit eigener Postleitzahl",
            Address = "Teststrasse 10",
            MunicipalityId = municipalityId,
            PostalCode = enteredPostalCode,
            Price = 300_000,
            Type = "House",
            Description = "Ausfuehrliche Beschreibung des Testhauses mit eigener PLZ und mehr als fuenfzig Zeichen.",
            LivingAreaSquareMeters = 110,
            Rooms = 4,
            Features = Array.Empty<string>(),
            ImageUrls = new[] { "/uploads/testhaus.jpg" }
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var createJson = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var propertyId = createJson.RootElement.GetProperty("PropertyId").GetGuid();

        // Detail liefert die eingegebene PLZ, nicht die der Gemeinde
        var detailResponse = await Client.GetAsync($"/api/properties/{propertyId}");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var detailJson = JsonDocument.Parse(await detailResponse.Content.ReadAsStringAsync());
        detailJson.RootElement
            .GetProperty("Property")
            .GetProperty("PostalCode")
            .GetString()
            .Should()
            .Be(enteredPostalCode);
    }

    [Test]
    public async Task ForeclosureListItem_CarriesAuctionDateAndMinimumBidAsPrice()
    {
        var accessToken = await RegisterSellerAsync("zv");
        var (municipalityId, _) = await GetFirstMunicipalityAsync();

        var auctionDate = DateTime.UtcNow.AddDays(30);
        const decimal minimumBid = 180_000m;

        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
        var createResponse = await Client.PostAsJsonAsync("/api/properties", new
        {
            Title = "Zwangsversteigerung Testobjekt Listendaten",
            Address = "Teststrasse 11",
            MunicipalityId = municipalityId,
            Price = minimumBid,
            Type = "Foreclosure",
            Description = "Ausfuehrliche Beschreibung der Test-Zwangsversteigerung mit deutlich mehr als fuenfzig Zeichen.",
            Features = Array.Empty<string>(),
            ImageUrls = new[] { "/uploads/zv.jpg" },
            TypeSpecificData = new Dictionary<string, object>
            {
                ["CourtName"] = "BG Testgericht",
                ["AuctionDate"] = auctionDate.ToString("O"),
                ["MinimumBid"] = minimumBid,
                ["EstimatedValue"] = 240_000m,
                ["Encumbrances"] = Array.Empty<object>(),
                ["Status"] = "Scheduled",
                ["FileNumber"] = "1 E 23/26x"
            }
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var createJson = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var propertyId = createJson.RootElement.GetProperty("PropertyId").GetGuid();

        // PropertyTypesJson erwartet Enum-Zahlenwerte ("[3]" = Foreclosure)
        var listResponse = await Client.GetAsync("/api/properties?PropertyTypesJson=%5B3%5D&PageSize=50");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var listJson = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
        var item = listJson.RootElement
            .GetProperty("Properties")
            .EnumerateArray()
            .FirstOrDefault(entry => entry.GetProperty("Id").GetGuid() == propertyId);

        item.ValueKind.Should().Be(JsonValueKind.Object, "die erstellte ZV muss in der Liste erscheinen");
        // WEB-011: Auktionstermin ist im Listen-DTO belegt
        item.GetProperty("AuctionDate").GetDateTime()
            .Should().BeCloseTo(auctionDate, TimeSpan.FromMinutes(1));
        // WEB-007/014: Price wird serverseitig aus dem Mindestgebot befuellt
        item.GetProperty("Price").GetDecimal().Should().Be(minimumBid);
    }

    [Test]
    public async Task CreateForeclosureWithPastAuctionDate_IsRejected()
    {
        var accessToken = await RegisterSellerAsync("zvpast");
        var (municipalityId, _) = await GetFirstMunicipalityAsync();

        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
        var createResponse = await Client.PostAsJsonAsync("/api/properties", new
        {
            Title = "Zwangsversteigerung mit vergangenem Termin",
            Address = "Teststrasse 12",
            MunicipalityId = municipalityId,
            Price = 150_000,
            Type = "Foreclosure",
            Description = "Ausfuehrliche Beschreibung dieser unzulaessigen Test-Zwangsversteigerung mit mehr als fuenfzig Zeichen.",
            Features = Array.Empty<string>(),
            ImageUrls = new[] { "/uploads/zv.jpg" },
            TypeSpecificData = new Dictionary<string, object>
            {
                ["CourtName"] = "BG Testgericht",
                ["AuctionDate"] = DateTime.UtcNow.AddDays(-5).ToString("O"),
                ["MinimumBid"] = 150_000m,
                ["Encumbrances"] = Array.Empty<object>(),
                ["Status"] = "Scheduled",
                ["FileNumber"] = "1 E 24/26y"
            }
        });

        // WEB-017: vergangene Versteigerungstermine sind beim Erstellen unzulaessig
        createResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<(Guid Id, string PostalCode)> GetFirstMunicipalityAsync()
    {
        var locationsResponse = await Client.GetAsync("/api/locations");
        locationsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var locationsJson = JsonDocument.Parse(await locationsResponse.Content.ReadAsStringAsync());
        var municipality = locationsJson.RootElement
            .GetProperty("FederalProvinces")[0]
            .GetProperty("Districts")[0]
            .GetProperty("Municipalities")[0];
        return (
            municipality.GetProperty("Id").GetGuid(),
            municipality.TryGetProperty("PostalCode", out var plz) ? plz.GetString() ?? "" : "");
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
