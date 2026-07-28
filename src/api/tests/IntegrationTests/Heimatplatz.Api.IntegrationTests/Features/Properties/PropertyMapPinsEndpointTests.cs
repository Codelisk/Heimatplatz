using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Properties.Contracts;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Heimatplatz.Api.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Heimatplatz.Api.IntegrationTests.Features.Properties;

/// <summary>
/// End-to-End-Tests der Kartenansicht (GET /api/properties/map-pins):
/// - nur Treffer MIT Koordinaten werden Pins, der Rest zaehlt als WithoutCoordinates
/// - ungenaue Lagen (IsLocationExact=false) werden serverseitig gestreut,
///   exakte (Nutzer-Opt-in "Genau") bleiben punktgenau; ZV bleibt bewusst ungefaehr
/// - der Endpoint teilt die Filterlogik mit der Listen-Suche (PropertyQueryFilters)
/// </summary>
[TestFixture]
[Category(TestCategories.Endpoint)]
[Category(TestCategories.Integration)]
public class PropertyMapPinsEndpointTests : BaseApiIntegrationTest
{
    private const string Password = "Passwort123!";
    private const double TestLatitude = 48.3069;
    private const double TestLongitude = 14.2858;

    protected override WebApplicationFactory<Program> CreateFactory()
        => new SqliteWebApplicationFactory<Program>();

    [Test]
    public async Task MapPins_ListsOnlyPropertiesWithCoordinates_AndJittersApproximateLocations()
    {
        var propertyId = await CreateHouseAsync("Kartenhaus ohne Koordinaten zuerst");

        // Ohne Koordinaten (Geocoding ist in Tests aus): kein Pin, aber gezaehlt
        using (var before = await GetMapPinsAsync())
        {
            FindPin(before, propertyId).Should().BeNull("ohne Koordinaten darf kein Pin erscheinen");
            before.RootElement.GetProperty("WithoutCoordinates").GetInt32().Should().BeGreaterThan(0);
            before.RootElement.GetProperty("Total").GetInt32().Should().BeGreaterThan(0);
        }

        // Ungenaue Lage: Pin erscheint, aber deterministisch gestreut (Privatsphaere)
        await SetCoordinatesAsync(propertyId, TestLatitude, TestLongitude, isExact: false);
        using var after = await GetMapPinsAsync();
        var pin = FindPin(after, propertyId);
        pin.Should().NotBeNull();

        pin!.Value.GetProperty("IsApproximate").GetBoolean().Should().BeTrue();
        var latitude = pin.Value.GetProperty("Latitude").GetDouble();
        var longitude = pin.Value.GetProperty("Longitude").GetDouble();
        latitude.Should().NotBe(TestLatitude, "die exakte Anschrift darf nicht ausgeliefert werden");
        // Streuband ~150-400 m: grob 0.006 Grad
        Math.Abs(latitude - TestLatitude).Should().BeLessThan(0.006);
        Math.Abs(longitude - TestLongitude).Should().BeLessThan(0.009);

        pin.Value.GetProperty("Title").GetString().Should().Be("Kartenhaus ohne Koordinaten zuerst");
        pin.Value.GetProperty("MunicipalityId").GetGuid().Should().NotBeEmpty();
        pin.Value.GetProperty("Price").GetDecimal().Should().Be(300_000m);
    }

    [Test]
    public async Task MapPins_ExactIntentWithExactGeocode_IsDeliveredUnchanged()
    {
        var propertyId = await CreateHouseAsync("Kartenhaus mit exakter Position");
        await SetCoordinatesAsync(propertyId, TestLatitude, TestLongitude, isExact: true, display: LocationDisplayMode.Exact);

        using var response = await GetMapPinsAsync();
        var pin = FindPin(response, propertyId);
        pin.Should().NotBeNull();
        pin!.Value.GetProperty("IsApproximate").GetBoolean().Should().BeFalse();
        pin.Value.GetProperty("Latitude").GetDouble().Should().Be(TestLatitude);
        pin.Value.GetProperty("Longitude").GetDouble().Should().Be(TestLongitude);
    }

    [Test]
    public async Task MapPins_ExactIntentWithoutExactGeocode_FallsBackToApproximate()
    {
        // Anbieter will "Genau", aber die Adresse war nur aufs Ortszentrum aufloesbar -
        // ehrlich bleiben: Kreis-Semantik samt Streuung statt falschem Punkt-Pin
        var propertyId = await CreateHouseAsync("Kartenhaus mit Genau-Wunsch ohne exakte Aufloesung");
        await SetCoordinatesAsync(propertyId, TestLatitude, TestLongitude, isExact: false, display: LocationDisplayMode.Exact);

        using var response = await GetMapPinsAsync();
        var pin = FindPin(response, propertyId);
        pin.Should().NotBeNull();
        pin!.Value.GetProperty("IsApproximate").GetBoolean().Should().BeTrue();
        pin.Value.GetProperty("Latitude").GetDouble().Should().NotBe(TestLatitude);
    }

    [Test]
    public async Task MapPins_HiddenDisplay_NeverAppearsOnTheMap()
    {
        var propertyId = await CreateHouseAsync("Kartenhaus mit verborgener Lage");
        await SetCoordinatesAsync(propertyId, TestLatitude, TestLongitude, isExact: true, display: LocationDisplayMode.Hidden);

        using var response = await GetMapPinsAsync();
        FindPin(response, propertyId).Should().BeNull("verborgene Lagen duerfen nie als Pin erscheinen");
        // Zaehlt wie ein Inserat ohne Kartenposition ("nur in der Liste")
        response.RootElement.GetProperty("WithoutCoordinates").GetInt32().Should().BeGreaterThan(0);
        // Auch der Einzelabruf der Detailseiten-Mini-Karte liefert nichts
        using var single = await GetMapPinsAsync($"?PropertyId={propertyId}");
        single.RootElement.GetProperty("Pins").GetArrayLength().Should().Be(0);
    }

    [Test]
    public async Task MapPins_SharesTypeFilterWithListSearch()
    {
        var propertyId = await CreateHouseAsync("Kartenhaus fuer den Typ-Filter");
        await SetCoordinatesAsync(propertyId, TestLatitude, TestLongitude, isExact: false);

        // Gleiches Filterformat wie die Web-Suche (String-Enums)
        using (var landOnly = await GetMapPinsAsync("?PropertyTypesJson=%5B%22Land%22%5D"))
        {
            FindPin(landOnly, propertyId).Should().BeNull("ein Haus darf den Land-Filter nicht passieren");
        }

        using var houseOnly = await GetMapPinsAsync("?PropertyTypesJson=%5B%22House%22%5D");
        FindPin(houseOnly, propertyId).Should().NotBeNull("der House-Filter muss das Haus liefern");
    }

    private async Task<JsonDocument> GetMapPinsAsync(string query = "")
    {
        var response = await Client.GetAsync($"/api/properties/map-pins{query}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    private static JsonElement? FindPin(JsonDocument document, Guid propertyId)
    {
        foreach (var pin in document.RootElement.GetProperty("Pins").EnumerateArray())
        {
            if (pin.GetProperty("Id").GetGuid() == propertyId)
                return pin;
        }
        return null;
    }

    private async Task SetCoordinatesAsync(
        Guid propertyId,
        double latitude,
        double longitude,
        bool isExact,
        LocationDisplayMode display = LocationDisplayMode.Approximate)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var property = await dbContext.Set<Property>().FirstAsync(p => p.Id == propertyId);
        property.Latitude = latitude;
        property.Longitude = longitude;
        property.IsLocationExact = isExact;
        property.LocationDisplay = display;
        await dbContext.SaveChangesAsync();
    }

    private async Task<Guid> CreateHouseAsync(string title)
    {
        var accessToken = await RegisterSellerAsync("map");
        var municipalityId = await GetFirstMunicipalityIdAsync();

        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
        var createResponse = await Client.PostAsJsonAsync("/api/properties", new
        {
            Title = title,
            Address = "Kartenstrasse 7",
            MunicipalityId = municipalityId,
            Price = 300_000,
            Type = "House",
            Description = "Ausfuehrliche Beschreibung des Karten-Testhauses mit deutlich mehr als fuenfzig Zeichen Inhalt.",
            LivingAreaSquareMeters = 120,
            Rooms = 4,
            Features = Array.Empty<string>(),
            ImageUrls = new[] { "/uploads/kartenhaus.jpg" }
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var createJson = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        return createJson.RootElement.GetProperty("PropertyId").GetGuid();
    }

    private async Task<Guid> GetFirstMunicipalityIdAsync()
    {
        var locationsResponse = await Client.GetAsync("/api/locations");
        locationsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var locationsJson = JsonDocument.Parse(await locationsResponse.Content.ReadAsStringAsync());
        return locationsJson.RootElement
            .GetProperty("FederalProvinces")[0]
            .GetProperty("Districts")[0]
            .GetProperty("Municipalities")[0]
            .GetProperty("Id")
            .GetGuid();
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
