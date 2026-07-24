using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Heimatplatz.Api.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace Heimatplatz.Api.IntegrationTests.Features.Feedback;

/// <summary>
/// End-to-End-Regressionstest fuer WEB-027: unbekannte oder fremde
/// Feedback-Tickets liefern HTTP 404 statt 200 mit null.
/// </summary>
[TestFixture]
[Category(TestCategories.Endpoint)]
[Category(TestCategories.Integration)]
public class FeedbackTicketEndpointTests : BaseApiIntegrationTest
{
    protected override WebApplicationFactory<Program> CreateFactory()
        => new SqliteWebApplicationFactory<Program>();

    [Test]
    public async Task GetUnknownFeedbackTicket_ReturnsNotFound()
    {
        var registerResponse = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            FirstName = "Max",
            LastName = "Mustermann",
            Email = $"feedback-{Guid.NewGuid():N}@heimatplatz.dev",
            Password = "Passwort123!"
        });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var registerJson = JsonDocument.Parse(await registerResponse.Content.ReadAsStringAsync());
        var accessToken = registerJson.RootElement.GetProperty("AccessToken").GetString();

        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await Client.GetAsync($"/api/feedback/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
