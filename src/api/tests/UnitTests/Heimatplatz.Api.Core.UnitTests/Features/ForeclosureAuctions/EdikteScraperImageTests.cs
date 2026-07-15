using System.Net;
using FluentAssertions;
using Heimatplatz.Api.Features.ForeclosureAuctions.Configuration;
using Heimatplatz.Api.Features.ForeclosureAuctions.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Heimatplatz.Api.Core.UnitTests.Features.ForeclosureAuctions;

[TestFixture]
public class EdikteScraperImageTests
{
    [Test]
    public async Task GetAuctionDetailAsync_ExtractsOriginalDimensionsAndPhotoMetadata()
    {
        const string html = """
            <html><body>
              <div class="row">
                <span class="col-sm-3">Termin:</span>
                <p class="col-sm-9">31.12.2030, 10:00</p>
              </div>
              <a href="/edikte/ex/exedi3.nsf/0/abc/$file/Lageplan.jpg"
                 onclick="imgwin('/edikte/ex/exedi3.nsf/0/abc/$file/Lageplan.jpg',2480,3507)"
                 title="Lageplan (855 KB)">Lageplan</a>
              <a href="/edikte/ex/exedi3.nsf/0/def/$file/Wohnhaus.JPG"
                 onclick="imgwin('/edikte/ex/exedi3.nsf/0/def/$file/Wohnhaus.JPG',2736,1824)"
                 title="Wohnhaus von Süden (4105 KB)">
                <img src="/edikte/ex/exedi3.nsf/0/def/$file/th1wohnhaus.jpg"
                     alt="Wohnhaus von Süden (4105 KB)" width="80" height="53">
              </a>
            </body></html>
            """;

        using var httpClient = new HttpClient(new StaticHtmlHandler(html));
        var scraper = new EdikteScraper(
            httpClient,
            Options.Create(new ScrapingOptions { DelayBetweenRequestsMs = 0 }),
            NullLogger<EdikteScraper>.Instance);

        var detail = await scraper.GetAuctionDetailAsync("0123456789abcdef");

        detail.ImageCandidates.Should().HaveCount(2);
        var photo = detail.ImageCandidates.Single(candidate => candidate.IsPhoto);
        photo.Url.Should().EndWith("/$file/Wohnhaus.JPG");
        photo.Width.Should().Be(2736);
        photo.Height.Should().Be(1824);
        photo.AltText.Should().Contain("Wohnhaus von Süden");
        detail.ImageUrls[0].Should().Be(photo.Url, "Fotos werden vor Plananhaengen einsortiert");
    }

    private sealed class StaticHtmlHandler(string html) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(html)
        });
    }
}
