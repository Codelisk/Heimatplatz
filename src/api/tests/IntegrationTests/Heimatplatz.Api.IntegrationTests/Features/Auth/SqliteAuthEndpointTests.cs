using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Heimatplatz.Api.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace Heimatplatz.Api.IntegrationTests.Features.Auth;

/// <summary>
/// Auth-Regressionstests gegen den lokal verwendeten SQLite-Provider.
/// Provider-spezifische Query-Probleme werden von den regulaeren InMemory-Tests
/// nicht erkannt.
/// </summary>
[TestFixture]
[Category(TestCategories.Auth)]
[Category(TestCategories.Endpoint)]
[Category(TestCategories.Integration)]
public class SqliteAuthEndpointTests : BaseApiIntegrationTest
{
    private const string Password = "Passwort123!";

    protected override WebApplicationFactory<Program> CreateFactory()
        => new SqliteWebApplicationFactory<Program>();

    [Test]
    public async Task RefreshToken_RotatesSuccessfully()
    {
        var email = $"sqlite-refresh-{Guid.NewGuid():N}@heimatplatz.dev";
        var registerResponse = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            FirstName = "Max",
            LastName = "Mustermann",
            Email = email,
            Password
        });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var registerJson = JsonDocument.Parse(await registerResponse.Content.ReadAsStringAsync());
        var refreshToken = registerJson.RootElement.GetProperty("RefreshToken").GetString();

        var refreshResponse = await Client.PostAsJsonAsync("/api/auth/refresh", new
        {
            RefreshToken = refreshToken
        });

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var refreshJson = JsonDocument.Parse(await refreshResponse.Content.ReadAsStringAsync());
        refreshJson.RootElement.GetProperty("RefreshToken").GetString().Should().NotBe(refreshToken);
    }
}
