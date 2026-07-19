using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Telemetry.Contracts.Mediator.Models;
using Heimatplatz.Api.Features.Telemetry.Data.Entities;
using Heimatplatz.Api.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Heimatplatz.Api.IntegrationTests.Features.Telemetry;

/// <summary>
/// End-to-End-Tests fuer den anonymen Client-Log-Ingest (MAUI-Crash-Reports).
/// Laeuft gegen die InMemory-Datenbank (appsettings.Testing.json) - der Handler
/// schreibt bewusst direkt ueber den AppDbContext, unabhaengig von der OTel-Pipeline.
/// </summary>
[TestFixture]
[Category(TestCategories.Endpoint)]
[Category(TestCategories.Integration)]
public class IngestClientLogsEndpointTests : BaseApiIntegrationTest
{
    // Die InMemory-Datenbank wird prozessweit geteilt - eigene Eintraege
    // deshalb ueber einen eindeutigen Message-Marker wiederfinden
    private static string UniqueMarker() => $"ingest-test-{Guid.NewGuid():N}";

    [Test]
    public async Task IngestClientLogs_ValidBatch_PersistsLogs()
    {
        var marker = UniqueMarker();

        var response = await Client.PostAsJsonAsync("/api/telemetry/client-logs", new
        {
            Source = "Maui",
            AppVersion = "1.0-test",
            Platform = "TestOS",
            Entries = new[]
            {
                new { TimestampUtc = DateTimeOffset.UtcNow, Level = "Warning", Message = marker }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var log = await dbContext.Set<TelemetryLog>().SingleAsync(l => l.Message == marker);

        log.Source.Should().Be(TelemetrySource.Maui);
        log.Category.Should().Be("Client");
        log.Level.Should().Be("Warning");
        log.ClientApp.Should().Be("Maui/1.0-test");
        log.ErrorGroupId.Should().BeNull();
    }

    [Test]
    public async Task IngestClientLogs_EntryWithException_CreatesAndLinksErrorGroup()
    {
        var marker = UniqueMarker();

        var response = await Client.PostAsJsonAsync("/api/telemetry/client-logs", new
        {
            Source = "Maui",
            AppVersion = "1.0-test",
            Platform = "TestOS",
            Entries = new[]
            {
                new
                {
                    TimestampUtc = DateTimeOffset.UtcNow,
                    Level = "Error",
                    Message = marker,
                    ExceptionText = $"System.InvalidOperationException: {marker}\n   at Client.App.Boom() in /app/MainPage.cs:line 12"
                }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var log = await dbContext.Set<TelemetryLog>().SingleAsync(l => l.Message == marker);

        log.ExceptionType.Should().Be("System.InvalidOperationException");
        log.ErrorGroupId.Should().NotBeNull();

        var group = await dbContext.Set<TelemetryErrorGroup>().SingleAsync(g => g.Id == log.ErrorGroupId);
        group.ExceptionType.Should().Be("System.InvalidOperationException");
        group.OccurrenceCount.Should().BeGreaterThanOrEqualTo(1);
        group.Status.Should().Be(ErrorGroupStatus.Open);
    }

    [Test]
    public async Task IngestClientLogs_BatchOverCap_ReturnsBadRequest()
    {
        var entries = Enumerable.Range(0, 21)
            .Select(i => new { TimestampUtc = DateTimeOffset.UtcNow, Level = "Information", Message = $"flood-{i}" })
            .ToArray();

        var response = await Client.PostAsJsonAsync("/api/telemetry/client-logs", new
        {
            Source = "Maui",
            AppVersion = "1.0-test",
            Platform = "TestOS",
            Entries = entries
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task IngestClientLogs_EmptyBatch_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync("/api/telemetry/client-logs", new
        {
            Source = "Maui",
            AppVersion = "1.0-test",
            Platform = "TestOS",
            Entries = Array.Empty<object>()
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
