using FluentAssertions;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Telemetry.Configuration;
using Heimatplatz.Api.Features.Telemetry.Data.Entities;
using Heimatplatz.Api.Features.Telemetry.Infrastructure;
using Heimatplatz.Api.UnitTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Heimatplatz.Api.Core.UnitTests.Features.Telemetry;

/// <summary>
/// Retention: alte Logs/Spans werden getrimmt, Fehlergruppen bleiben dauerhaft.
/// Laeuft auf InMemory (= der Nicht-Postgres-Pfad ohne ExecuteDelete).
/// </summary>
[TestFixture]
[Category(TestCategories.Unit)]
[Category(TestCategories.Data)]
public class TelemetryRetentionWorkerTests : BaseApiUnitTest
{
    private AppDbContext dbContext = null!;

    [SetUp]
    public void SetUpDbContext()
    {
        // Telemetry-Assembly VOR dem Modellbau laden (Entity-Auto-Discovery scannt
        // nur bereits geladene Heimatplatz-Assemblies)
        _ = typeof(TelemetrySpan).Assembly;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"telemetry-retention-{Guid.NewGuid():N}")
            .Options;
        dbContext = new AppDbContext(options);
    }

    [TearDown]
    public void TearDownDbContext()
    {
        dbContext.Dispose();
    }

    [Test]
    public async Task TrimOnceAsync_RemovesOnlyExpiredRowsAndKeepsErrorGroups()
    {
        var now = DateTimeOffset.UtcNow;
        var options = new TelemetryOptions
        {
            RetentionDays = { Logs = 30, Spans = 14 }
        };

        var group = new TelemetryErrorGroup
        {
            Id = Guid.CreateVersion7(),
            FingerprintHash = new string('a', 64),
            ExceptionType = "System.Exception",
            Title = "System.Exception: alt",
            SampleMessage = "alt",
            FirstSeenUtc = now.AddDays(-90),
            LastSeenUtc = now.AddDays(-60),
            OccurrenceCount = 3
        };
        dbContext.Add(group);
        dbContext.Add(NewLog(now.AddDays(-40)));
        dbContext.Add(NewLog(now.AddDays(-1)));
        dbContext.Add(NewSpan(now.AddDays(-20)));
        dbContext.Add(NewSpan(now.AddHours(-1)));
        await dbContext.SaveChangesAsync();

        var (removedLogs, removedSpans) =
            await TelemetryRetentionWorker.TrimOnceAsync(dbContext, options, now, CancellationToken.None);

        removedLogs.Should().Be(1);
        removedSpans.Should().Be(1);
        (await dbContext.Set<TelemetryLog>().CountAsync()).Should().Be(1);
        (await dbContext.Set<TelemetrySpan>().CountAsync()).Should().Be(1);
        (await dbContext.Set<TelemetryErrorGroup>().CountAsync()).Should().Be(1, "Fehlergruppen werden nie getrimmt");
    }

    [Test]
    public async Task TrimOnceAsync_NothingExpired_RemovesNothing()
    {
        var now = DateTimeOffset.UtcNow;
        dbContext.Add(NewLog(now.AddDays(-5)));
        dbContext.Add(NewSpan(now.AddDays(-5)));
        await dbContext.SaveChangesAsync();

        var (removedLogs, removedSpans) = await TelemetryRetentionWorker.TrimOnceAsync(
            dbContext, new TelemetryOptions(), now, CancellationToken.None);

        removedLogs.Should().Be(0);
        removedSpans.Should().Be(0);
    }

    private static TelemetryLog NewLog(DateTimeOffset timestamp) => new()
    {
        Id = Guid.CreateVersion7(),
        TimestampUtc = timestamp,
        Level = "Warning",
        Category = "Test",
        Message = "Testeintrag"
    };

    private static TelemetrySpan NewSpan(DateTimeOffset start) => new()
    {
        Id = Guid.CreateVersion7(),
        TraceId = new string('b', 32),
        SpanId = "0011223344556677",
        Name = "Test",
        Kind = "Server",
        StartTimeUtc = start,
        StatusCode = "Unset"
    };
}
