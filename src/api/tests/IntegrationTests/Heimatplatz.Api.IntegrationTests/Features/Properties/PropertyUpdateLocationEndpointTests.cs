using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Properties.Contracts;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Heimatplatz.Api.Features.Properties.Services;
using Heimatplatz.Api.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Heimatplatz.Api.IntegrationTests.Features.Properties;

/// <summary>
/// End-to-End-Tests des Lage-Nachziehens beim Bearbeiten (PUT /api/properties):
/// Der Opt-in auf "Genau" muss auch OHNE Adressaenderung nachgeocodiert werden,
/// sonst liefert map-pins trotz gespeichertem LocationDisplay=Exact weiterhin
/// IsApproximate=true (QA-Befund B-03 vom 29.07.2026).
/// </summary>
[TestFixture]
[Category(TestCategories.Endpoint)]
[Category(TestCategories.Integration)]
public class PropertyUpdateLocationEndpointTests : BaseApiIntegrationTest
{
    private const string Password = "Passwort123!";
    private const double StaleLatitude = 48.2;
    private const double StaleLongitude = 14.25;
    private const double ExactLatitude = 48.3069;
    private const double ExactLongitude = 14.2858;

    private FakePropertyGeocoder _geocoder = null!;

    protected override WebApplicationFactory<Program> CreateFactory()
    {
        _geocoder = new FakePropertyGeocoder();
        return new SqliteWebApplicationFactory<Program>(
            configureTestServices: services => services.AddSingleton<IPropertyGeocoder>(_geocoder));
    }

    [Test]
    public async Task Update_ExactOptInWithoutAddressChange_RegeocodesToExactPin()
    {
        // Altbestand: Koordinaten vorhanden, aber nur ungefaehr aufgeloest
        var (propertyId, municipalityId) = await CreateHouseAsync("Genau-Opt-in ohne Adressaenderung");
        await SetStoredLocationAsync(propertyId, isExact: false, LocationDisplayMode.Approximate);

        _geocoder.Result = new PropertyGeocodeResult(ExactLatitude, ExactLongitude, IsExact: true);
        await UpdateHouseAsync(propertyId, municipalityId, locationDisplay: "Exact");

        var property = await LoadPropertyAsync(propertyId);
        property.LocationDisplay.Should().Be(LocationDisplayMode.Exact);
        property.IsLocationExact.Should().BeTrue("der Genau-Opt-in muss die Aufloesungsqualitaet nachziehen");
        property.Latitude.Should().Be(ExactLatitude);
        property.Longitude.Should().Be(ExactLongitude);

        using var pins = await GetMapPinsAsync(propertyId);
        var pin = FindPin(pins, propertyId);
        pin.Should().NotBeNull();
        pin!.Value.GetProperty("IsApproximate").GetBoolean().Should().BeFalse();
        pin.Value.GetProperty("Latitude").GetDouble().Should().Be(ExactLatitude);
    }

    [Test]
    public async Task Update_ExactOptInWhenGeocoderFails_KeepsStoredCoordinates()
    {
        var (propertyId, municipalityId) = await CreateHouseAsync("Genau-Opt-in bei Geocoder-Ausfall");
        await SetStoredLocationAsync(propertyId, isExact: false, LocationDisplayMode.Approximate);

        _geocoder.Result = null; // Nominatim nicht erreichbar/nichts aufloesbar
        await UpdateHouseAsync(propertyId, municipalityId, locationDisplay: "Exact");

        var property = await LoadPropertyAsync(propertyId);
        property.LocationDisplay.Should().Be(LocationDisplayMode.Exact);
        property.IsLocationExact.Should().BeFalse();
        property.Latitude.Should().Be(StaleLatitude, "ohne Adressaenderung bleiben die alten Koordinaten stehen");

        // Ehrliche Anzeige: weiterhin Umgebungskreis-Semantik samt Streuung
        using var pins = await GetMapPinsAsync(propertyId);
        var pin = FindPin(pins, propertyId);
        pin.Should().NotBeNull();
        pin!.Value.GetProperty("IsApproximate").GetBoolean().Should().BeTrue();
    }

    [Test]
    public async Task Update_AlreadyExactWithoutAddressChange_DoesNotGeocodeAgain()
    {
        var (propertyId, municipalityId) = await CreateHouseAsync("Bereits exakt gespeicherte Lage");
        await SetStoredLocationAsync(propertyId, isExact: true, LocationDisplayMode.Exact);

        var callsBefore = _geocoder.CallCount;
        await UpdateHouseAsync(propertyId, municipalityId, locationDisplay: "Exact");

        _geocoder.CallCount.Should().Be(callsBefore, "unveraendert exakte Lagen brauchen keinen neuen Nominatim-Request");
        (await LoadPropertyAsync(propertyId)).IsLocationExact.Should().BeTrue();
    }

    private sealed class FakePropertyGeocoder : IPropertyGeocoder
    {
        public PropertyGeocodeResult? Result { get; set; }
        public int CallCount { get; private set; }

        public Task<PropertyGeocodeResult?> GeocodeAsync(
            string? street, string? postalCode, string city, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(Result);
        }
    }

    private async Task<Property> LoadPropertyAsync(Guid propertyId)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await dbContext.Set<Property>().AsNoTracking().FirstAsync(p => p.Id == propertyId);
    }

    private async Task SetStoredLocationAsync(Guid propertyId, bool isExact, LocationDisplayMode display)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var property = await dbContext.Set<Property>().FirstAsync(p => p.Id == propertyId);
        property.Latitude = StaleLatitude;
        property.Longitude = StaleLongitude;
        property.IsLocationExact = isExact;
        property.LocationDisplay = display;
        await dbContext.SaveChangesAsync();
    }

    private async Task UpdateHouseAsync(Guid propertyId, Guid municipalityId, string locationDisplay)
    {
        // Adresse/Gemeinde bewusst identisch zum Anlegen: der Befund betrifft
        // genau den Fall "nur der Anzeige-Modus aendert sich"
        var response = await Client.PutAsJsonAsync("/api/properties", new
        {
            Id = propertyId,
            Title = "Kartenhaus fuer den Lage-Update-Test",
            Address = "Kartenstrasse 7",
            MunicipalityId = municipalityId,
            Price = 300_000,
            Type = "House",
            Description = "Ausfuehrliche Beschreibung des Karten-Testhauses mit deutlich mehr als fuenfzig Zeichen Inhalt.",
            Features = Array.Empty<string>(),
            ImageUrls = new[] { "/uploads/kartenhaus.jpg" },
            LocationDisplay = locationDisplay
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<JsonDocument> GetMapPinsAsync(Guid propertyId)
    {
        var response = await Client.GetAsync($"/api/properties/map-pins?PropertyId={propertyId}");
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

    private async Task<(Guid PropertyId, Guid MunicipalityId)> CreateHouseAsync(string title)
    {
        var accessToken = await RegisterSellerAsync();
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
        return (createJson.RootElement.GetProperty("PropertyId").GetGuid(), municipalityId);
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

    private async Task<string> RegisterSellerAsync()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            FirstName = "Max",
            LastName = "Mustermann",
            Email = $"lage-{Guid.NewGuid():N}@heimatplatz.dev",
            Password,
            SellerType = "Private"
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("AccessToken").GetString()
            ?? throw new InvalidOperationException("Registrierung lieferte kein Access Token.");
    }
}
