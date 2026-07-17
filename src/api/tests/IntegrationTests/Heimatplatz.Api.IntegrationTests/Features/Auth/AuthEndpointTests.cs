using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Heimatplatz.Api.IntegrationTests.Infrastructure;
using NUnit.Framework;

namespace Heimatplatz.Api.IntegrationTests.Features.Auth;

/// <summary>
/// End-to-End-Tests fuer den Auth-Flow: Registrierung, Login, Profil.
/// Laeuft gegen die InMemory-Datenbank (appsettings.Testing.json).
/// </summary>
[TestFixture]
[Category(TestCategories.Auth)]
[Category(TestCategories.Endpoint)]
[Category(TestCategories.Integration)]
public class AuthEndpointTests : BaseApiIntegrationTest
{
    private const string Password = "Passwort123!";

    // Die InMemory-Datenbank wird prozessweit ueber den Namen geteilt -
    // jede Registrierung braucht deshalb eine eindeutige E-Mail-Adresse.
    private static string UniqueEmail() => $"test-{Guid.NewGuid():N}@heimatplatz.dev";

    [Test]
    [Category(TestCategories.Smoke)]
    public async Task Register_ReturnsAccessToken()
    {
        var response = await RegisterAsync(UniqueEmail());

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("AccessToken").GetString().Should().NotBeNullOrEmpty();
        json.RootElement.GetProperty("RefreshToken").GetString().Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var email = UniqueEmail();

        var first = await RegisterAsync(email);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await RegisterAsync(email);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var email = UniqueEmail();
        await RegisterAsync(email);

        var response = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = email,
            Password = "Falsches-Passwort-99!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Login_WithCorrectPassword_ReturnsAccessToken()
    {
        var email = UniqueEmail();
        await RegisterAsync(email);

        var response = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = email,
            Password
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("AccessToken").GetString().Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task RefreshToken_RotatesAndDetectsReuse()
    {
        var email = UniqueEmail();
        var registerResponse = await RegisterAsync(email);
        using var reg = JsonDocument.Parse(await registerResponse.Content.ReadAsStringAsync());
        var refreshToken = reg.RootElement.GetProperty("RefreshToken").GetString();

        // Erster Refresh: Rotation liefert ein neues Token-Paar
        var first = await Client.PostAsJsonAsync("/api/auth/refresh", new { RefreshToken = refreshToken });
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        using var firstJson = JsonDocument.Parse(await first.Content.ReadAsStringAsync());
        var rotated = firstJson.RootElement.GetProperty("RefreshToken").GetString();
        rotated.Should().NotBe(refreshToken);

        // Reuse des bereits rotierten Tokens: 401 ...
        var reuse = await Client.PostAsJsonAsync("/api/auth/refresh", new { RefreshToken = refreshToken });
        reuse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // ... und die Reuse-Detection hat die ganze Token-Familie widerrufen -
        // auch der rotierte Nachfolger ist nicht mehr verwendbar
        var afterReuse = await Client.PostAsJsonAsync("/api/auth/refresh", new { RefreshToken = rotated });
        afterReuse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetProfile_WithoutToken_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/auth/profile");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetProfile_WithToken_ReturnsOwnProfile()
    {
        var email = UniqueEmail();
        var registerResponse = await RegisterAsync(email);
        using var registerJson = JsonDocument.Parse(await registerResponse.Content.ReadAsStringAsync());
        var accessToken = registerJson.RootElement.GetProperty("AccessToken").GetString();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await Client.GetAsync("/api/auth/profile");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("Email").GetString().Should().Be(email);
    }

    private Task<HttpResponseMessage> RegisterAsync(string email) =>
        Client.PostAsJsonAsync("/api/auth/register", new
        {
            FirstName = "Max",
            LastName = "Mustermann",
            Email = email,
            Password
        });
}
