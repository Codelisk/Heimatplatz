using FluentAssertions;
using Heimatplatz.Api.Features.Dashboards.Services.Charts;
using NUnit.Framework;

namespace Heimatplatz.Api.UnitTests.Features.Dashboards;

[TestFixture]
public class DashboardChartRendererTests
{
    private static readonly byte[] PngMagic = [0x89, 0x50, 0x4E, 0x47];

    private static byte[] DecodeDataUri(string dataUri)
    {
        dataUri.Should().StartWith("data:image/png;base64,");
        return Convert.FromBase64String(dataUri["data:image/png;base64,".Length..]);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void PriceHistogram_RendersPngInBothThemes(bool dark)
    {
        var renderer = new DashboardChartRenderer();
        var prices = new List<decimal> { 95_000, 245_000, 289_000, 349_000, 365_000, 425_000, 890_000 };

        var bytes = DecodeDataUri(renderer.RenderPriceHistogramDataUri(prices, dark));

        bytes.Take(4).Should().Equal(PngMagic);
        bytes.Length.Should().BeGreaterThan(1_000, "ein leeres Bild wäre kleiner");
    }

    [Test]
    public void PriceHistogram_SinglePriceStillRenders()
    {
        var renderer = new DashboardChartRenderer();

        var bytes = DecodeDataUri(renderer.RenderPriceHistogramDataUri([250_000m], dark: false));

        bytes.Take(4).Should().Equal(PngMagic);
    }

    [Test]
    public void NewPerWeek_RendersPngWithFixedNow()
    {
        var renderer = new DashboardChartRenderer();
        var now = new DateTime(2026, 8, 6, 12, 0, 0, DateTimeKind.Utc);
        var dates = Enumerable.Range(0, 30).Select(i => now.AddDays(-i * 2)).ToList();

        var bytes = DecodeDataUri(renderer.RenderNewPerWeekDataUri(dates, dark: true, now));

        bytes.Take(4).Should().Equal(PngMagic);
    }

    [TestCase(100_000, 900_000, 7, 200_000)]
    [TestCase(0, 70_000, 7, 10_000)]
    [TestCase(0, 100, 7, 20)]
    public void NiceBucketWidth_ProducesRoundSteps(int min, int max, int buckets, int expected)
    {
        DashboardChartRenderer.NiceBucketWidth(min, max, buckets).Should().Be(expected);
    }

    [TestCase(450_000, "450.000")]
    [TestCase(1_250_000, "1,25 Mio.")]
    [TestCase(2_000_000, "2 Mio.")]
    public void FormatEuroCompact_FormatsAxisLabels(int value, string expected)
    {
        DashboardChartRenderer.FormatEuroCompact(value).Should().Be(expected);
    }

    [Test]
    public void StartOfIsoWeek_ReturnsMonday()
    {
        // 6.8.2026 ist ein Donnerstag -> Montag ist der 3.8.
        DashboardChartRenderer.StartOfIsoWeek(new DateTime(2026, 8, 6, 15, 30, 0, DateTimeKind.Utc))
            .Should().Be(new DateTime(2026, 8, 3));
    }
}
