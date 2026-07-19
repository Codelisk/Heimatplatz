using FluentAssertions;
using Heimatplatz.Api.Features.Telemetry.Configuration;
using Heimatplatz.Api.Features.Telemetry.Data.Entities;
using Heimatplatz.Api.Features.Telemetry.Infrastructure;
using Heimatplatz.Api.UnitTests.Infrastructure;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Heimatplatz.Api.Core.UnitTests.Features.Telemetry;

/// <summary>
/// Tail-Sampling-Puffer: Fehlermarkierung, Caps und Sweep verwaister Traces.
/// </summary>
[TestFixture]
[Category(TestCategories.Unit)]
[Category(TestCategories.Fast)]
public class TraceBufferServiceTests : BaseApiUnitTest
{
    private static TraceBufferService CreateService(Action<TelemetryOptions>? configure = null)
    {
        var options = new TelemetryOptions();
        configure?.Invoke(options);
        return new TraceBufferService(Options.Create(options));
    }

    private static TelemetrySpan Span(string traceId) => new()
    {
        Id = Guid.CreateVersion7(),
        TraceId = traceId,
        SpanId = "0011223344556677",
        Name = "Test",
        Kind = "Internal",
        StatusCode = "Unset"
    };

    private static TelemetryLog Log(string traceId) => new()
    {
        Id = Guid.CreateVersion7(),
        TraceId = traceId,
        Level = "Information",
        Category = "Test",
        Message = "Kontext"
    };

    [Test]
    public void TryRemove_HealthyTrace_ReturnsBucketWithoutErrorFlag()
    {
        var service = CreateService();
        service.AddSpan("trace-1", Span("trace-1"));
        service.AddContextLog("trace-1", Log("trace-1"));

        var bucket = service.TryRemove("trace-1");

        bucket.Should().NotBeNull();
        bucket!.HasError.Should().BeFalse();
        bucket.Spans.Should().HaveCount(1);
        bucket.Logs.Should().HaveCount(1);

        // Zweiter Zugriff: Puffer ist entnommen
        service.TryRemove("trace-1").Should().BeNull();
    }

    [Test]
    public void MarkError_BeforeRootEnd_BucketCarriesErrorAndContextLogs()
    {
        var service = CreateService();
        service.AddContextLog("trace-err", Log("trace-err"));
        service.MarkError("trace-err");
        service.AddSpan("trace-err", Span("trace-err"));

        var bucket = service.TryRemove("trace-err");

        bucket.Should().NotBeNull();
        bucket!.HasError.Should().BeTrue();
        bucket.Logs.Should().HaveCount(1, "Kontext-Logs muessen beim Fehler-Trace nachgereicht werden");
        bucket.Spans.Should().HaveCount(1);
    }

    [Test]
    public void AddSpan_OverPerTraceCap_DropsExcess()
    {
        var service = CreateService(o => o.MaxSpansPerTrace = 2);

        for (var i = 0; i < 5; i++)
            service.AddSpan("trace-cap", Span("trace-cap"));

        service.TryRemove("trace-cap")!.Spans.Should().HaveCount(2);
    }

    [Test]
    public void AddSpan_OverTotalTraceCap_IgnoresNewTraces()
    {
        var service = CreateService(o => o.MaxBufferedTraces = 1);

        service.AddSpan("trace-a", Span("trace-a"));
        service.AddSpan("trace-b", Span("trace-b"));

        service.TryRemove("trace-a").Should().NotBeNull();
        service.TryRemove("trace-b").Should().BeNull("ueber dem Gesamt-Cap darf kein neuer Trace gepuffert werden");
    }

    [Test]
    public void SweepAbandoned_FlushesErroredBucketsAndDropsHealthy()
    {
        var service = CreateService(o => o.AbandonedTraceTimeoutMinutes = 2);
        service.AddSpan("trace-healthy", Span("trace-healthy"));
        service.AddSpan("trace-error", Span("trace-error"));
        service.MarkError("trace-error");

        var flushed = service.SweepAbandoned(DateTimeOffset.UtcNow.AddMinutes(5));

        flushed.Should().HaveCount(1);
        flushed[0].HasError.Should().BeTrue();

        // Beide Buckets sind nach dem Sweep entfernt
        service.TryRemove("trace-healthy").Should().BeNull();
        service.TryRemove("trace-error").Should().BeNull();
    }

    [Test]
    public void SweepAbandoned_YoungBuckets_AreKept()
    {
        var service = CreateService(o => o.AbandonedTraceTimeoutMinutes = 2);
        service.AddSpan("trace-young", Span("trace-young"));

        service.SweepAbandoned(DateTimeOffset.UtcNow).Should().BeEmpty();
        service.TryRemove("trace-young").Should().NotBeNull();
    }
}
