using System.Globalization;
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
/// Das Datum der Bekanntmachung ist die Quelle fuer "Eingestellt am" im Inserat.
/// Die Ediktsdatei liefert es in zwei sich ausschliessenden Varianten - beide muessen
/// treffen, sonst faellt das Inserat still auf den Scrape-Zeitpunkt zurueck.
/// </summary>
[TestFixture]
public class EdikteScraperPublicationDateTests
{
    // Variante A: Edikt ohne Aenderungsprotokoll. Datum steht als beschriftete Zeile.
    // Der Block "Alle Edikte zum Fall" steht danach und ist damit der letzte div im
    // druckbereich - genau daran ist der frueher verwendete Selektor gescheitert.
    private const string HtmlWithLabelledRow = """
        <html><body><div id="druckbereich">
          <div class="row"><span class="col-sm-3 text-right">Dienststelle:</span><p class="col-sm-9">BG Kirchdorf an der Krems (491)</p></div>
          <div class="row"><span class="col-sm-3 text-right">Bekannt gemacht am:</span><p class="col-sm-9">12.06.2026</p></div>
          <div class="row"><span class="col-sm-3 text-right">Versteigerungstermin:</span><p class="col-sm-9"><strong>am 29.07.2026 um 10:00 Uhr</strong></p></div>
          <p><strong>Alle Edikte zum Fall:</strong></p>
          <div class="row"><span class="col-sm-3"><span>Versteigerung Wohnhaus mit Garage (29.07.2026 10:00)</span></span><p class="col-sm-9">4643 Pettenbach, D&uuml;rnbachweg 27</p></div>
        </div></body></html>
        """;

    // Variante B: Edikt mit Aenderungsprotokoll. Keine beschriftete Zeile, das Datum
    // steht ohne fuehrende Null im Protokoll-Block.
    private const string HtmlWithChangeLog = """
        <html><body><div id="druckbereich">
          <div class="row"><span class="col-sm-3 text-right">Dienststelle:</span><p class="col-sm-9">BG Wels (401)</p></div>
          <div class="edibody">
            <a id="e2"></a><p class="edibekannt">Bekannt gemacht am 25.6.2026</p>
            <p class="edititle">Sonstiges Edikt</p>
            <p class="editext">"Ort und Zeit der Besichtigung" hinzugef&uuml;gt</p>
          </div>
        </div></body></html>
        """;

    [Test]
    public async Task GetAuctionDetailAsync_ReadsPublicationDateFromLabelledRow()
    {
        var detail = await ScrapeAsync(HtmlWithLabelledRow);

        detail.PublicationDateText.Should().Be("12.06.2026");
    }

    // Variante B mit mehreren Protokoll-Eintraegen: alle Eintraege stehen chronologisch
    // aufsteigend in EINEM div.edibody (live verifiziert, Anker e2/e3/e4). "Eingestellt am"
    // muss die aelteste sichtbare Bekanntmachung sein, nicht die letzte Aenderung.
    private const string HtmlWithMultiEntryChangeLog = """
        <html><body><div id="druckbereich">
          <div class="page-header text-center"><h1><small>Versteigerung - Objekt 1</small></h1></div>
          <div class="row"><span class="col-sm-3 text-right">Dienststelle:</span><p class="col-sm-9">BG Vöcklabruck (415)</p></div>
          <div class="edibody">
            <a id="e2"></a><p class="edibekannt">Bekannt gemacht am 25.6.2026</p>
            <p class="edititle">Sonstiges Edikt</p>
            <p class="editext">"Sonstiges" ge&auml;ndert</p><hr>
            <a id="e3"></a><p class="edibekannt">Bekannt gemacht am 9.7.2026</p>
            <p class="edititle">Sonstiges Edikt</p>
            <p class="editext">"Ort und Zeit der Besichtigung" hinzugef&uuml;gt</p><hr>
          </div>
        </div></body></html>
        """;

    [Test]
    public async Task GetAuctionDetailAsync_ReadsPublicationDateFromChangeLog()
    {
        var detail = await ScrapeAsync(HtmlWithChangeLog);

        detail.PublicationDateText.Should().Be("Bekannt gemacht am 25.6.2026");
    }

    [Test]
    public async Task GetAuctionDetailAsync_ReadsOldestEntryFromMultiEntryChangeLog()
    {
        var detail = await ScrapeAsync(HtmlWithMultiEntryChangeLog);

        detail.PublicationDateText.Should().Be("Bekannt gemacht am 25.6.2026");
    }

    [Test]
    public async Task GetAuctionDetailAsync_TakesStatusFromPageTitleNotFromChangeLog()
    {
        var detail = await ScrapeAsync(HtmlWithMultiEntryChangeLog);

        // Frueher lieferte der aelteste Protokolleintrag den Status ("Sonstiges Edikt") -
        // der Edikt-Typ steht aber im Seitentitel.
        detail.Title.Should().Be("Versteigerung - Objekt 1");
        detail.StatusText.Should().Be("Versteigerung - Objekt 1");
    }

    [TestCase("12.06.2026", "2026-06-12")]
    [TestCase("Bekannt gemacht am 25.6.2026", "2026-06-25")]
    public void ParsePublicationDate_KeepsCalendarDayInEveryDisplayTimeZone(string text, string expectedDay)
    {
        var parsed = ForeclosureAuctionSyncService.ParsePublicationDate(text);

        parsed.Should().NotBeNull();

        // Web-SSR laeuft im Container auf UTC, MAUI formatiert den DateTimeOffset direkt.
        // Auf Mitternacht verankert waere hier jeweils der Vortag zu sehen.
        parsed!.Value.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            .Should().Be(expectedDay);

        var vienna = TimeZoneInfo.ConvertTime(parsed.Value, TimeZoneInfo.FindSystemTimeZoneById("Europe/Vienna"));
        vienna.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture).Should().Be(expectedDay);
    }

    [Test]
    public void ParsePublicationDate_ReturnsNullWithoutDate()
    {
        ForeclosureAuctionSyncService.ParsePublicationDate("Bekannt gemacht am").Should().BeNull();
        ForeclosureAuctionSyncService.ParsePublicationDate(null).Should().BeNull();
    }

    private static async Task<EdiktDetail> ScrapeAsync(string html)
    {
        var scraper = new EdikteScraper(
            new HttpClient(new StubHandler(html)),
            Options.Create(new ScrapingOptions { BaseUrl = "https://edikte.justiz.gv.at", DelayBetweenRequestsMs = 0 }),
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
