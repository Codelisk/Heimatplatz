using System.Net;
using System.Text;
using FluentAssertions;
using Heimatplatz.Api.Features.Firmenbuch.Configuration;
using Heimatplatz.Api.Features.Firmenbuch.Services;
using Heimatplatz.Api.UnitTests.Infrastructure;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Heimatplatz.Api.UnitTests.Features.Firmenbuch;

/// <summary>
/// Tests fuer das Mapping der Firmenpool-Antwort (GET /api/firmenbuch/companies). Die Fixture
/// entspricht der am 30.7.2026 live abgerufenen Antwortstruktur: camelCase, "aufrecht" als
/// status:null, Sichtungszeitpunkte als ISO-8601 mit Offset.
/// </summary>
[TestFixture]
[Category(TestCategories.Unit)]
public class FirmenpoolApiClientTests : BaseApiUnitTest
{
    private const string RealWorldResponse = """
        {
          "items": [
            {
              "fnr": "612447h",
              "name": "0815pasta e.U.",
              "status": null,
              "sitz": "Traun",
              "rechtsformCode": "EU",
              "rechtsformText": "Einzelunternehmer",
              "rechtseigenschaft": null,
              "gerichtCode": "458",
              "gerichtText": "Landesgericht Linz",
              "sourceOrtNr": "4",
              "statementCount": 0,
              "latestStichtag": null,
              "latestUmsatzerloese": null,
              "latestRohergebnis": null,
              "latestBilanzsumme": null,
              "firstSeenAt": "2026-07-25T12:49:58.079898+00:00",
              "lastSeenAt": "2026-07-29T19:00:15.004009+00:00"
            },
            {
              "fnr": "112708y",
              "name": "S & J IMMOBILIEN GmbH",
              "status": "gelöscht",
              "sitz": "Mattighofen",
              "rechtsformCode": "GES",
              "rechtsformText": "Gesellschaft mit beschränkter Haftung",
              "rechtseigenschaft": null,
              "gerichtCode": "440",
              "gerichtText": "Landesgericht Ried im Innkreis",
              "sourceOrtNr": "4",
              "statementCount": 3,
              "latestStichtag": "2015-12-31",
              "latestUmsatzerloese": null,
              "latestRohergebnis": 12345.67,
              "latestBilanzsumme": 98765.43,
              "firstSeenAt": "2026-07-29T17:12:03.000000+00:00",
              "lastSeenAt": "2026-07-29T19:00:15.004009+00:00"
            }
          ],
          "totalCount": 84585,
          "page": 3,
          "pageSize": 2
        }
        """;

    [Test]
    public async Task GetCompaniesAsync_mappt_die_Firmenpool_Antwort_vollstaendig()
    {
        var client = CreateClient(RealWorldResponse, out var requests);

        var page = await client.GetCompaniesAsync(new FirmenpoolCompanyQuery
        {
            Page = 3,
            PageSize = 2,
            Status = "aufrecht",
            NameContainsAny = "immo,liegenschaft",
            ExcludeFnrs = "91180p"
        });

        requests.Should().ContainSingle()
            .Which.Should().Be(
                "https://firmenpool.test/api/firmenbuch/companies?Page=3&PageSize=2" +
                "&Status=aufrecht&NameContainsAny=immo%2Cliegenschaft&ExcludeFnrs=91180p");

        page.TotalCount.Should().Be(84585);
        page.Page.Should().Be(3);
        page.PageSize.Should().Be(2);
        page.Items.Should().HaveCount(2);

        var aufrecht = page.Items[0];
        aufrecht.Fnr.Should().Be("612447h");
        aufrecht.Name.Should().Be("0815pasta e.U.");
        aufrecht.Status.Should().BeNull("aufrecht ist in der Quelle das Fehlen eines Vermerks");
        aufrecht.Sitz.Should().Be("Traun");
        aufrecht.RechtsformCode.Should().Be("EU");
        aufrecht.RechtsformText.Should().Be("Einzelunternehmer");
        aufrecht.Rechtseigenschaft.Should().BeNull();
        aufrecht.GerichtCode.Should().Be("458");
        aufrecht.GerichtText.Should().Be("Landesgericht Linz");
        aufrecht.SourceOrtNr.Should().Be("4");
        aufrecht.FirstSeenAt.Should().Be(DateTimeOffset.Parse("2026-07-25T12:49:58.079898+00:00"));
        aufrecht.LastSeenAt.Should().Be(DateTimeOffset.Parse("2026-07-29T19:00:15.004009+00:00"));

        var geloescht = page.Items[1];
        geloescht.Status.Should().Be("gelöscht");
        geloescht.Name.Should().Be("S & J IMMOBILIEN GmbH");
    }

    [Test]
    public async Task GetCompaniesAsync_wirft_bei_Fehlerstatus()
    {
        var client = CreateClient("Nicht freigegeben.", out _, HttpStatusCode.Forbidden);

        var act = () => client.GetCompaniesAsync(new FirmenpoolCompanyQuery { Page = 1, PageSize = 200 });

        // 403 heisst hier: die eigene IP fehlt in der Caddy-Freischaltung des Firmenpools -
        // das soll den Lauf laut scheitern lassen, nicht als leere Seite durchrutschen.
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    private const string DetailResponse = """
        {
          "fnr": "359925b",
          "name": "M & I Bau GmbH",
          "status": null,
          "euid": "ATBRA.359925-000",
          "gegruendet": null,
          "strasse": "Teststrasse",
          "hausnummer": "12",
          "plz": "4061",
          "ort": "Pasching",
          "staat": "AUT",
          "sitz": "Pasching",
          "rechtsformCode": "GES",
          "rechtsformText": "Gesellschaft mit beschränkter Haftung",
          "gerichtText": "Landesgericht Linz",
          "handelsregisternummer": null,
          "auszugStand": "2026-07-29T19:23:26.43687+00:00",
          "abschluesseVorhanden": 14,
          "funktionaere": [
            { "name": "Ivica Marusic", "funktionCode": "GF", "funktionText": "GESCHÄFTSFÜHRER/IN (handelsrechtlich)", "seit": "2016-11-09", "aktiv": true }
          ],
          "gewerbe": [
            { "gisaZahl": 15255228, "schluessel": "502", "wortlaut": "Baugewerbetreibender", "plz": "4061", "ort": "Pasching", "weitereStandorte": ["4020 Linz"], "aktiv": true }
          ]
        }
        """;

    [Test]
    public async Task GetCompanyDetailAsync_mappt_Auszug_Funktionaere_und_Gewerbe()
    {
        var client = CreateClient(DetailResponse, out var requests);

        var detail = await client.GetCompanyDetailAsync(" 359925b ");

        requests.Should().ContainSingle()
            .Which.Should().Be("https://firmenpool.test/api/firmenbuch/companies/359925b");

        detail.Should().NotBeNull();
        detail!.Name.Should().Be("M & I Bau GmbH");
        detail.Euid.Should().Be("ATBRA.359925-000");
        detail.Plz.Should().Be("4061");
        detail.AbschluesseVorhanden.Should().Be(14);
        detail.Funktionaere.Should().ContainSingle()
            .Which.Should().Match<FirmenpoolOfficer>(f =>
                f.Name == "Ivica Marusic" && f.Aktiv && f.Seit == new DateOnly(2016, 11, 9));
        detail.Gewerbe.Should().ContainSingle()
            .Which.WeitereStandorte.Should().ContainSingle("4020 Linz");
    }

    [Test]
    public async Task GetCompanyDetailAsync_liefert_null_bei_unbekannter_Fnr()
    {
        // 404 ist ein normaler Fall (Tippfehler, Firma ausserhalb OOe) und darf den
        // Aufrufer nicht mit einer Exception treffen.
        var client = CreateClient("{}", out _, HttpStatusCode.NotFound);

        var detail = await client.GetCompanyDetailAsync("999999x");

        detail.Should().BeNull();
    }

    private static FirmenpoolApiClient CreateClient(
        string responseBody,
        out List<string> capturedUrls,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        List<string> urls = [];
        capturedUrls = urls;

        var handler = new StubHandler(request =>
        {
            urls.Add(request.RequestUri!.ToString());
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            };
        });

        var options = Options.Create(new FirmenpoolOptions { BaseUrl = "https://firmenpool.test" });
        return new FirmenpoolApiClient(new HttpClient(handler), options);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }
}
