using System.Net;
using System.Text;
using FluentAssertions;
using Heimatplatz.Api.Features.ForeclosureAuctions.Configuration;
using Heimatplatz.Api.Features.ForeclosureAuctions.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Heimatplatz.Api.Core.UnitTests.Features.ForeclosureAuctions;

/// <summary>
/// Die Ediktsdatei verlinkt Bilder wurzel-relativ ("/edikte/..."). Solche Pfade MUESSEN gegen
/// die BaseUrl aufgeloest werden - der Bild-Proxy laesst ausschliesslich https durch und reicht
/// alles andere ungeproxt an den Browser weiter.
///
/// Achtung beim Bewerten dieser Tests: Uri.TryCreate(url, UriKind.Absolute, ...) ist NICHT
/// plattformneutral. Auf Linux (= Prod-Container) parst ein fuehrender "/" erfolgreich als
/// Dateisystempfad zu file:///edikte/..., auf Windows schlaegt derselbe Aufruf fehl. Ein Fix
/// hier kann also auf einer Windows-Entwicklermaschine gruen sein und trotzdem auf Prod
/// falsche URLs schreiben - die Tests greifen dafuer auf Linux-CI.
/// </summary>
[TestFixture]
public class EdikteScraperImageUrlTests
{
    private const string BaseUrl = "https://edikte.justiz.gv.at";

    // Regulaeres Edikt: Vollbild-Link mit eingebettetem th1-Thumbnail, beide wurzel-relativ.
    private const string HtmlWithRootRelativeImage = """
        <html><body><div id="druckbereich">
          <a href="/edikte/ex/exedi3.nsf/0/abc/$file/DSC08559.JPG" title="Ansicht">
            <img src="/edikte/ex/exedi3.nsf/0/abc/$file/th1DSC08559.JPG" alt="Ansicht" />
          </a>
        </div></body></html>
        """;

    // Edikt, das denselben Anhang bereits absolut verlinkt - muss unveraendert bleiben.
    private const string HtmlWithAbsoluteImage = """
        <html><body><div id="druckbereich">
          <a href="https://edikte.justiz.gv.at/edikte/ex/exedi3.nsf/0/abc/$file/DSC08559.JPG">
            <img src="https://edikte.justiz.gv.at/edikte/ex/exedi3.nsf/0/abc/$file/th1DSC08559.JPG" alt="Ansicht" />
          </a>
        </div></body></html>
        """;

    [Test]
    public async Task GetAuctionDetailAsync_ResolvesRootRelativeImageAgainstBaseUrl()
    {
        var detail = await ScrapeAsync(HtmlWithRootRelativeImage);

        detail.ImageUrls.Should().ContainSingle()
            .Which.Should().Be($"{BaseUrl}/edikte/ex/exedi3.nsf/0/abc/$file/DSC08559.JPG");
    }

    [Test]
    public async Task GetAuctionDetailAsync_KeepsAbsoluteImageUrl()
    {
        var detail = await ScrapeAsync(HtmlWithAbsoluteImage);

        detail.ImageUrls.Should().ContainSingle()
            .Which.Should().Be($"{BaseUrl}/edikte/ex/exedi3.nsf/0/abc/$file/DSC08559.JPG");
    }

    [TestCase(HtmlWithRootRelativeImage)]
    [TestCase(HtmlWithAbsoluteImage)]
    public async Task GetAuctionDetailAsync_NeverEmitsNonHttpImageUrl(string html)
    {
        var detail = await ScrapeAsync(html);

        detail.ImageUrls.Should().NotBeEmpty();
        detail.ImageUrls.Should().AllSatisfy(url =>
            url.Should().StartWith("https://", "der Bild-Proxy reicht alles andere ungeproxt durch"));
    }

    private static async Task<EdiktDetail> ScrapeAsync(string html)
    {
        var scraper = new EdikteScraper(
            new HttpClient(new StubHandler(html)),
            Options.Create(new ScrapingOptions { BaseUrl = BaseUrl, DelayBetweenRequestsMs = 0 }),
            NullLogger<EdikteScraper>.Instance);

        return await scraper.GetAuctionDetailAsync("dummy-id");
    }

    private sealed class StubHandler(string html) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(html, Encoding.UTF8, "text/html")
            });
    }
}
