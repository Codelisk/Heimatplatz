using FluentAssertions;
using Heimatplatz.Api.Features.Properties.Handlers;
using NUnit.Framework;

namespace Heimatplatz.Api.UnitTests.Features.Properties;

[TestFixture]
public class PropertyChangesSinceParsingTests
{
    [Test]
    public void TryParseSince_ReadsIsoRoundtripValue()
    {
        var parsed = GetPropertyChangesHandler.TryParseSince("2026-07-28T15:49:35.6715840+00:00", out var since);

        parsed.Should().BeTrue();
        since.ToUniversalTime().Should().Be(new DateTimeOffset(2026, 7, 28, 15, 49, 35, TimeSpan.Zero).AddTicks(6_715_840));
    }

    [Test]
    public void TryParseSince_ReadsUtcZValue()
    {
        var parsed = GetPropertyChangesHandler.TryParseSince("2026-07-28T15:49:35.6715840Z", out var since);

        parsed.Should().BeTrue();
        since.ToUniversalTime().Should().Be(new DateTimeOffset(2026, 7, 28, 15, 49, 35, TimeSpan.Zero).AddTicks(6_715_840));
    }

    /// <summary>
    /// Der generierte Shiny-HTTP-Client haengt Query-Werte unkodiert an die URL - aus
    /// "+00:00" wird beim Dekodieren serverseitig " 00:00". Ohne Reparatur bekaeme jeder
    /// betroffene Client bei jedem Sync einen Voll-Refresh gemeldet.
    /// </summary>
    [TestCase("2026-07-28T15:49:35.6715840 00:00", 0)]
    [TestCase("2026-07-28T17:49:35.6715840 02:00", 2)]
    public void TryParseSince_RepairsZoneSignLostInQueryString(string raw, int offsetHours)
    {
        var parsed = GetPropertyChangesHandler.TryParseSince(raw, out var since);

        parsed.Should().BeTrue();
        since.Offset.Should().Be(TimeSpan.FromHours(offsetHours));
        since.ToUniversalTime().Should().Be(new DateTimeOffset(2026, 7, 28, 15, 49, 35, TimeSpan.Zero).AddTicks(6_715_840));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("kein-datum")]
    public void TryParseSince_RejectsUnusableValues(string? raw)
    {
        GetPropertyChangesHandler.TryParseSince(raw, out _).Should().BeFalse();
    }
}
