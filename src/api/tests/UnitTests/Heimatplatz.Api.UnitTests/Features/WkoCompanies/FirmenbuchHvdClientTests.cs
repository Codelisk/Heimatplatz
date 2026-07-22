using System.Xml.Linq;
using FluentAssertions;
using Heimatplatz.Api.Features.WkoCompanies.Services;
using Heimatplatz.Api.UnitTests.Infrastructure;
using NUnit.Framework;

namespace Heimatplatz.Api.UnitTests.Features.WkoCompanies;

/// <summary>
/// Tests fuer das Parsen der FBW-WebServices-(HVD)-Antworten (AUSZUG_V2). Die Fixture bildet
/// die am 22.7.2026 live abgerufene Antwortstruktur nach (anonymisiert): PNR/FKENTEXT kommen
/// als ATTRIBUTE am FUN/PER-Element (nicht als Kind-Elemente wie das XSD nahelegt), DATERST
/// als yyyyMMdd in FI_DKZ09, VOLLZUGSDATUM als ISO-Datum, EUID doppelt verschachtelt.
/// </summary>
[TestFixture]
[Category(TestCategories.Unit)]
public class FirmenbuchHvdClientTests : BaseApiUnitTest
{
    private const string RealWorldResponse = """
        <env:Envelope xmlns:env="http://www.w3.org/2003/05/soap-envelope">
          <env:Header/>
          <env:Body>
            <ns6:AUSZUG_V2_RESPONSE xmlns:ns6="ns://firmenbuch.justiz.gv.at/Abfrage/v2/AuszugResponse"
                ns6:FNR="120121 z" ns6:STICHTAG="2026-07-22" ns6:UMFANG="Kurzinformation">
              <ns6:FIRMA>
                <ns6:FI_DKZ02 ns6:AUFRECHT="true" ns6:VNR="020">
                  <ns6:BEZEICHNUNG>Muster Immobilien GmbH</ns6:BEZEICHNUNG>
                </ns6:FI_DKZ02>
                <ns6:FI_DKZ09 ns6:AUFRECHT="true" ns6:VNR="001">
                  <ns6:HRAHRB>HRB 15287</ns6:HRAHRB>
                  <ns6:DATERST>19730802</ns6:DATERST>
                </ns6:FI_DKZ09>
                <ns6:FI_DKZ09 ns6:AUFRECHT="false" ns6:VNR="002">
                  <ns6:DATERST></ns6:DATERST>
                </ns6:FI_DKZ09>
              </ns6:FIRMA>
              <ns6:FUN ns6:FKEN="GF" ns6:FKENTEXT="GESCHÄFTSFÜHRER/IN (handelsrechtlich)" ns6:PNR="  M">
                <ns6:FU_DKZ10 ns6:AUFRECHT="true" ns6:VNR="020">
                  <ns6:DATVON>20040722</ns6:DATVON>
                </ns6:FU_DKZ10>
              </ns6:FUN>
              <ns6:FUN ns6:FKEN="PR" ns6:FKENTEXT="PROKURIST/IN" ns6:PNR="  U">
                <ns6:FU_DKZ10 ns6:AUFRECHT="true" ns6:VNR="048">
                  <ns6:DATVON>20240102</ns6:DATVON>
                </ns6:FU_DKZ10>
              </ns6:FUN>
              <ns6:PER ns6:PNR="  M">
                <ns6:PE_DKZ02 ns6:AUFRECHT="true" ns6:VNR="008">
                  <ns6:TITELVOR>Mag.</ns6:TITELVOR>
                  <ns6:VORNAME>Maxima</ns6:VORNAME>
                  <ns6:NACHNAME>Musterfrau</ns6:NACHNAME>
                  <ns6:NAME_FORMATIERT>Mag. Maxima Musterfrau</ns6:NAME_FORMATIERT>
                  <ns6:GEBURTSDATUM>19690420</ns6:GEBURTSDATUM>
                </ns6:PE_DKZ02>
              </ns6:PER>
              <ns6:PER ns6:PNR="  U">
                <ns6:PE_DKZ02 ns6:AUFRECHT="true" ns6:VNR="048">
                  <ns6:VORNAME>Max</ns6:VORNAME>
                  <ns6:NACHNAME>Mustermann</ns6:NACHNAME>
                  <ns6:NAME_FORMATIERT>Max Mustermann</ns6:NAME_FORMATIERT>
                </ns6:PE_DKZ02>
              </ns6:PER>
              <ns6:VOLLZ>
                <ns6:VNR>001</ns6:VNR>
                <ns6:VOLLZUGSDATUM>1994-09-29</ns6:VOLLZUGSDATUM>
                <ns6:ANTRAGSTEXT>Ersterfassung gem. Art. XXIII Abs. 4 FBG</ns6:ANTRAGSTEXT>
              </ns6:VOLLZ>
              <ns6:VOLLZ>
                <ns6:VNR>020</ns6:VNR>
                <ns6:VOLLZUGSDATUM>2004-08-03</ns6:VOLLZUGSDATUM>
              </ns6:VOLLZ>
              <ns6:EUID>
                <ns6:ZNR>000</ns6:ZNR>
                <ns6:EUID>ATBRA.120121-000</ns6:EUID>
              </ns6:EUID>
            </ns6:AUSZUG_V2_RESPONSE>
          </env:Body>
        </env:Envelope>
        """;

    private static XElement LoadAuszug(string xml) =>
        XDocument.Parse(xml).Descendants().First(e => e.Name.LocalName == "AUSZUG_V2_RESPONSE");

    [Test]
    public void ParseAuszug_RealWorldStructure_MapsEuidFoundedDateAndPeople()
    {
        var auszug = FirmenbuchHvdClient.ParseAuszug(LoadAuszug(RealWorldResponse));

        auszug.Euid.Should().Be("ATBRA.120121-000");

        // DATERST (Ersteintragung 1973) schlaegt das aelteste Vollzugsdatum (FBG-Ersterfassung 1994)
        auszug.FoundedDate.Should().Be(new DateOnly(1973, 8, 2));

        auszug.People.Should().HaveCount(2);

        var gf = auszug.People[0];
        gf.Name.Should().Be("Mag. Maxima Musterfrau");
        gf.BirthDate.Should().Be(new DateOnly(1969, 4, 20));
        gf.Role.Should().Be("GESCHÄFTSFÜHRER/IN (handelsrechtlich)");

        var prokurist = auszug.People[1];
        prokurist.Name.Should().Be("Max Mustermann");
        prokurist.BirthDate.Should().BeNull();
        prokurist.Role.Should().Be("PROKURIST/IN");
    }

    [Test]
    public void ParseAuszug_WithoutDaterst_FallsBackToOldestVollzugsdatum()
    {
        // XSD-Variante: PNR/FKENTEXT als Kind-Elemente statt Attribute - muss weiterhin matchen
        const string xml = """
            <ns6:AUSZUG_V2_RESPONSE xmlns:ns6="ns://firmenbuch.justiz.gv.at/Abfrage/v2/AuszugResponse">
              <ns6:FUN>
                <ns6:PNR>  A</ns6:PNR>
                <ns6:FKENTEXT>GESCHÄFTSFÜHRER/IN (handelsrechtlich)</ns6:FKENTEXT>
              </ns6:FUN>
              <ns6:PER>
                <ns6:PNR>  A</ns6:PNR>
                <ns6:PE_DKZ02>
                  <ns6:NAME_FORMATIERT>Erika Beispiel</ns6:NAME_FORMATIERT>
                  <ns6:GEBURTSDATUM>19800101</ns6:GEBURTSDATUM>
                </ns6:PE_DKZ02>
              </ns6:PER>
              <ns6:VOLLZ>
                <ns6:VOLLZUGSDATUM>2011-05-17</ns6:VOLLZUGSDATUM>
              </ns6:VOLLZ>
              <ns6:VOLLZ>
                <ns6:VOLLZUGSDATUM>2019-02-01</ns6:VOLLZUGSDATUM>
              </ns6:VOLLZ>
            </ns6:AUSZUG_V2_RESPONSE>
            """;

        var auszug = FirmenbuchHvdClient.ParseAuszug(LoadAuszug(xml));

        auszug.Euid.Should().BeNull();
        auszug.FoundedDate.Should().Be(new DateOnly(2011, 5, 17));
        auszug.People.Should().ContainSingle();
        auszug.People[0].Name.Should().Be("Erika Beispiel");
        auszug.People[0].Role.Should().Be("GESCHÄFTSFÜHRER/IN (handelsrechtlich)");
    }

    [TestCase("120121z", "120121z")]
    [TestCase("FN 120121 z", "120121z")]
    [TestCase(" fn120121z ", "120121z")]
    [TestCase("120121 z", "120121z")]
    public void NormalizeFnr_StripsPrefixAndWhitespace(string input, string expected)
    {
        FirmenbuchHvdClient.NormalizeFnr(input).Should().Be(expected);
    }
}
