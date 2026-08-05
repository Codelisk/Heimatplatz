using System.Text;
using System.Xml;
using FluentAssertions;
using Heimatplatz.Api.Features.OpenImmoImport.Models;
using Heimatplatz.Api.Features.OpenImmoImport.Services;
using Heimatplatz.Api.Features.Properties.Contracts;
using Heimatplatz.Api.Features.Properties.Contracts.Models.TypeSpecific;
using Heimatplatz.Api.Features.Properties.Contracts.Models.TypeSpecific.Enums;
using NUnit.Framework;

namespace Heimatplatz.Api.Core.UnitTests.Features.OpenImmoImport;

[TestFixture]
public class OpenImmoParserTests
{
    private OpenImmoParser _parser = null!;

    [SetUp]
    public void SetUp() => _parser = new OpenImmoParser();

    private OpenImmoParseResult Parse(string xml)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        return _parser.Parse(stream);
    }

    /// <summary>
    /// Realistische Justimmo-artige 1.2.7-Datei: 2 Haeuser, 1 Grundstueck,
    /// 1 Wohnung (Produktregel-Skip), 1 Mietobjekt (Skip), 1 DELETE-Aktion.
    /// </summary>
    private const string FullFixture = """
        <?xml version="1.0" encoding="UTF-8"?>
        <openimmo>
          <uebertragung art="OFFLINE" umfang="VOLL" sendersoftware="JUSTIMMO" version="1.2.7"/>
          <anbieter>
            <anid>IMMOBAER</anid>
            <immobilie>
              <objektkategorie>
                <nutzungsart WOHNEN="1" GEWERBE="0"/>
                <vermarktungsart KAUF="1" MIETE_PACHT="0"/>
                <objektart><haus haustyp="EINFAMILIENHAUS"/></objektart>
              </objektkategorie>
              <geo>
                <plz>4600</plz>
                <ort>Wels</ort>
                <strasse>Ringstraße</strasse>
                <hausnummer>12</hausnummer>
                <anzahl_etagen>2</anzahl_etagen>
                <geokoordinaten breitengrad="48.16123" laengengrad="14.03456"/>
              </geo>
              <kontaktperson>
                <email_direkt>max.huber@immobaer.at</email_direkt>
                <name>Huber</name>
                <vorname>Max</vorname>
                <tel_durchw>+43 660 1234567</tel_durchw>
              </kontaktperson>
              <preise>
                <kaufpreis>520000.50</kaufpreis>
              </preise>
              <flaechen>
                <wohnflaeche>145.7</wohnflaeche>
                <grundstuecksflaeche>850.4</grundstuecksflaeche>
                <anzahl_zimmer>4.5</anzahl_zimmer>
                <anzahl_schlafzimmer>3</anzahl_schlafzimmer>
                <anzahl_badezimmer>2</anzahl_badezimmer>
                <anzahl_terrassen>1</anzahl_terrassen>
              </flaechen>
              <ausstattung>
                <unterkellert keller="JA"/>
                <stellplatzart GARAGE="true"/>
                <gartennutzung>true</gartennutzung>
                <kamin>true</kamin>
              </ausstattung>
              <zustand_angaben>
                <baujahr>1998</baujahr>
                <zustand zustand_art="GEPFLEGT"/>
              </zustand_angaben>
              <freitexte>
                <objekttitel>Traumhaus in Wels mit großem Garten – Ähre für Öko-Käufer</objekttitel>
                <objektbeschreibung>Wunderschönes Einfamilienhaus.</objektbeschreibung>
                <lage>Ruhige Siedlungslage.</lage>
              </freitexte>
              <anhaenge>
                <anhang location="EXTERN" gruppe="BILD">
                  <anhangtitel>Garten</anhangtitel>
                  <format>jpg</format>
                  <daten><pfad>https://files.justimmo.at/public/pics/garten.jpg</pfad></daten>
                </anhang>
                <anhang location="EXTERN" gruppe="TITELBILD">
                  <anhangtitel>Front</anhangtitel>
                  <format>jpg</format>
                  <daten><pfad>https://files.justimmo.at/public/pics/front.jpg</pfad></daten>
                </anhang>
                <anhang location="EXTERN" gruppe="DOKUMENTE">
                  <anhangtitel>Exposé</anhangtitel>
                  <format>pdf</format>
                  <daten><pfad>https://files.justimmo.at/public/docs/expose.pdf</pfad></daten>
                </anhang>
              </anhaenge>
              <verwaltung_objekt>
                <objektadresse_freigeben>1</objektadresse_freigeben>
              </verwaltung_objekt>
              <verwaltung_techn>
                <openimmo_obid>OBID-001</openimmo_obid>
                <objektnr_extern>IB-2026-01</objektnr_extern>
                <aktion aktionart="CHANGE"/>
                <stand_vom>2026-07-30</stand_vom>
              </verwaltung_techn>
            </immobilie>
            <immobilie>
              <objektkategorie>
                <vermarktungsart KAUF="true"/>
                <objektart><haus haustyp="EINFAMILIENHAUS"/></objektart>
              </objektkategorie>
              <geo>
                <plz>4810</plz>
                <ort>Gmunden</ort>
                <strasse>Seeblickweg</strasse>
                <hausnummer>3</hausnummer>
              </geo>
              <preise><kaufpreis>199000</kaufpreis></preise>
              <anhaenge>
                <anhang gruppe="BILD">
                  <format>jpg</format>
                  <daten><anhanginhalt>QUJD</anhanginhalt></daten>
                </anhang>
              </anhaenge>
              <verwaltung_objekt>
                <objektadresse_freigeben>0</objektadresse_freigeben>
              </verwaltung_objekt>
              <verwaltung_techn>
                <objektnr_extern>EXT-77</objektnr_extern>
              </verwaltung_techn>
              <user_defined_simplefield feldname="url">https://apps.justimmo.at/website/objekt/EXT-77</user_defined_simplefield>
              <user_defined_simplefield feldname="geokoordinaten_breitengrad">47.91234</user_defined_simplefield>
              <user_defined_simplefield feldname="geokoordinaten_laengengrad">13.79876</user_defined_simplefield>
            </immobilie>
            <immobilie>
              <objektkategorie>
                <vermarktungsart KAUF="1"/>
                <objektart><grundstueck grundst_typ="WOHNEN"/></objektart>
              </objektkategorie>
              <geo>
                <plz>4614</plz>
                <ort>Marchtrenk</ort>
              </geo>
              <preise><kaufpreis>95000</kaufpreis></preise>
              <flaechen><grundstuecksflaeche>1200</grundstuecksflaeche></flaechen>
              <anhaenge>
                <anhang location="INTERN" gruppe="KARTEN_LAGEPLAN">
                  <format>png</format>
                  <daten><pfad>bilder/plan.png</pfad></daten>
                </anhang>
              </anhaenge>
              <verwaltung_techn>
                <openimmo_obid>OBID-003</openimmo_obid>
              </verwaltung_techn>
            </immobilie>
            <immobilie>
              <objektkategorie>
                <vermarktungsart KAUF="1"/>
                <objektart><wohnung wohnungtyp="ETAGE"/></objektart>
              </objektkategorie>
              <geo><plz>4020</plz><ort>Linz</ort></geo>
              <preise><kaufpreis>310000</kaufpreis></preise>
              <verwaltung_techn><openimmo_obid>OBID-004</openimmo_obid></verwaltung_techn>
            </immobilie>
            <immobilie>
              <objektkategorie>
                <vermarktungsart KAUF="0" MIETE_PACHT="1"/>
                <objektart><haus haustyp="EINFAMILIENHAUS"/></objektart>
              </objektkategorie>
              <geo><plz>4600</plz><ort>Wels</ort></geo>
              <preise><nettokaltmiete>1200</nettokaltmiete></preise>
              <verwaltung_techn><openimmo_obid>OBID-005</openimmo_obid></verwaltung_techn>
            </immobilie>
            <immobilie>
              <objektkategorie>
                <vermarktungsart KAUF="1"/>
                <objektart><haus/></objektart>
              </objektkategorie>
              <verwaltung_techn>
                <openimmo_obid>OBID-DEL</openimmo_obid>
                <aktion aktionart="DELETE"/>
              </verwaltung_techn>
            </immobilie>
          </anbieter>
        </openimmo>
        """;

    [Test]
    public void Parse_FullFixture_ImportsOnlyHausUndGrundstueckMitKauf()
    {
        var result = Parse(FullFixture);

        result.IsPartialTransfer.Should().BeFalse();
        result.Listings.Should().HaveCount(3);
        result.Listings.Select(l => l.SourceId).Should().BeEquivalentTo("OBID-001", "EXT-77", "OBID-003");
        // Wohnung + Miete uebersprungen
        result.SkippedCount.Should().Be(2);
        result.DeletedSourceIds.Should().BeEquivalentTo("OBID-DEL");
    }

    [Test]
    public void Parse_Haus_MapptAlleFelder()
    {
        var haus = Parse(FullFixture).Listings.Single(l => l.SourceId == "OBID-001");

        haus.Type.Should().Be(PropertyType.House);
        haus.Title.Should().Be("Traumhaus in Wels mit großem Garten – Ähre für Öko-Käufer");
        haus.Description.Should().Be("Wunderschönes Einfamilienhaus.\n\nRuhige Siedlungslage.");
        haus.Street.Should().Be("Ringstraße 12");
        haus.AddressReleased.Should().BeTrue();
        haus.PostalCode.Should().Be("4600");
        haus.City.Should().Be("Wels");
        haus.Latitude.Should().BeApproximately(48.16123, 0.00001);
        haus.Longitude.Should().BeApproximately(14.03456, 0.00001);
        haus.Price.Should().Be(520000.50m);
        haus.LivingAreaSquareMeters.Should().Be(146);
        haus.PlotAreaSquareMeters.Should().Be(850);
        haus.Rooms.Should().Be(5, "4,5 Zimmer werden kaufmaennisch gerundet");
        haus.Bedrooms.Should().Be(3);
        haus.Bathrooms.Should().Be(2);
        haus.Floors.Should().Be(2);
        haus.YearBuilt.Should().Be(1998);
        haus.IsNewBuildProject.Should().BeFalse("Bestandshaus mit vergangenem Baujahr");
        haus.Condition.Should().Be(PropertyCondition.Good);
        haus.HasBasement.Should().BeTrue();
        haus.HasGarage.Should().BeTrue();
        haus.HasGarden.Should().BeTrue();
        haus.Features.Should().Contain(["Keller", "Garage", "Garten", "Kamin", "Terrasse"]);
        haus.Contact.Should().NotBeNull();
        haus.Contact!.Name.Should().Be("Max Huber");
        haus.Contact.Email.Should().Be("max.huber@immobaer.at");
        haus.Contact.Phone.Should().Be("+43 660 1234567");
        haus.StandVom.Should().Be(new DateTimeOffset(2026, 7, 30, 0, 0, 0, TimeSpan.Zero));
    }

    [Test]
    public void Parse_Anhaenge_TitelbildZuerstUndDokumenteGefiltert()
    {
        var haus = Parse(FullFixture).Listings.Single(l => l.SourceId == "OBID-001");

        haus.Attachments.Should().HaveCount(2, "das PDF-Expose ist kein Bild");
        haus.Attachments[0].Location.Should().EndWith("front.jpg", "TITELBILD wird nach vorne sortiert");
        haus.Attachments[0].Mode.Should().Be(OpenImmoAttachmentMode.ExternalUrl);
        haus.Attachments[1].Location.Should().EndWith("garten.jpg");
    }

    [Test]
    public void Parse_ObjektnrExtern_AlsFallbackFuerFehlendeObid()
    {
        var haus = Parse(FullFixture).Listings.Single(l => l.SourceId == "EXT-77");

        haus.Title.Should().Be("Einfamilienhaus in Gmunden", "ohne objekttitel greift der Haustyp-Fallback");
        haus.AddressReleased.Should().BeFalse();
        haus.Street.Should().Be("Seeblickweg 3", "die Strasse wird geparst, aber erst der Sync entscheidet ueber die Anzeige");
        haus.Attachments.Should().ContainSingle().Which.Mode.Should().Be(OpenImmoAttachmentMode.Base64);
        haus.Attachments[0].Base64Content.Should().Be("QUJD");
    }

    [Test]
    public void Parse_UserDefinedFelder_LiefernUrlUndKoordinatenFallback()
    {
        var haus = Parse(FullFixture).Listings.Single(l => l.SourceId == "EXT-77");

        haus.ExternalUrl.Should().Be("https://apps.justimmo.at/website/objekt/EXT-77",
            "Justimmo liefert den Objektlink als user_defined_simplefield");
        haus.Latitude.Should().BeApproximately(47.91234, 0.00001,
            "ohne Standard-geokoordinaten greifen die user_defined-Koordinaten");
        haus.Longitude.Should().BeApproximately(13.79876, 0.00001);

        // Standard-Element hat weiterhin Vorrang (Haus 1 hat beide Wege nicht noetig)
        var haus1 = Parse(FullFixture).Listings.Single(l => l.SourceId == "OBID-001");
        haus1.Latitude.Should().BeApproximately(48.16123, 0.00001);
        haus1.ExternalUrl.Should().BeNull("Haus 1 hat kein url-Feld im Fixture");
    }

    [Test]
    public void Parse_Grundstueck_MitZipEntryUndZoning()
    {
        var grund = Parse(FullFixture).Listings.Single(l => l.SourceId == "OBID-003");

        grund.Type.Should().Be(PropertyType.Land);
        grund.Zoning.Should().Be(ZoningType.Residential);
        grund.PlotAreaSquareMeters.Should().Be(1200);
        grund.Attachments.Should().ContainSingle();
        grund.Attachments[0].Mode.Should().Be(OpenImmoAttachmentMode.ZipEntry);
        grund.Attachments[0].Location.Should().Be("bilder/plan.png");
    }

    [Test]
    public void Parse_MitDefaultNamespace_FunktioniertIdentisch()
    {
        var xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <openimmo xmlns="http://www.openimmo.de">
              <uebertragung art="OFFLINE" umfang="VOLL"/>
              <anbieter>
                <immobilie>
                  <objektkategorie>
                    <vermarktungsart KAUF="1"/>
                    <objektart><haus/></objektart>
                  </objektkategorie>
                  <geo><plz>4600</plz><ort>Wels</ort></geo>
                  <preise><kaufpreis>250000</kaufpreis></preise>
                  <verwaltung_techn><openimmo_obid>NS-1</openimmo_obid></verwaltung_techn>
                </immobilie>
              </anbieter>
            </openimmo>
            """;

        var result = Parse(xml);

        result.Listings.Should().ContainSingle().Which.SourceId.Should().Be("NS-1");
    }

    [Test]
    public void Parse_UmfangTeil_SetztPartialFlag()
    {
        var xml = """
            <openimmo>
              <uebertragung art="ONLINE" umfang="TEIL"/>
              <anbieter/>
            </openimmo>
            """;

        Parse(xml).IsPartialTransfer.Should().BeTrue();
    }

    [Test]
    public void Parse_KaufpreisAufAnfrage_WirdUebersprungen()
    {
        var xml = """
            <openimmo>
              <uebertragung umfang="VOLL"/>
              <anbieter>
                <immobilie>
                  <objektkategorie>
                    <vermarktungsart KAUF="1"/>
                    <objektart><haus/></objektart>
                  </objektkategorie>
                  <preise><kaufpreis auf_anfrage="1">0</kaufpreis></preise>
                  <verwaltung_techn><openimmo_obid>ANFRAGE-1</openimmo_obid></verwaltung_techn>
                </immobilie>
              </anbieter>
            </openimmo>
            """;

        var result = Parse(xml);

        result.Listings.Should().BeEmpty();
        result.SkippedCount.Should().Be(1);
        result.Warnings.Should().ContainMatch("*ANFRAGE-1*");
    }

    [Test]
    public void Parse_OhneJeglicheId_WirdUebersprungen()
    {
        var xml = """
            <openimmo>
              <uebertragung umfang="VOLL"/>
              <anbieter>
                <immobilie>
                  <objektkategorie>
                    <vermarktungsart KAUF="1"/>
                    <objektart><haus/></objektart>
                  </objektkategorie>
                  <preise><kaufpreis>100000</kaufpreis></preise>
                  <verwaltung_techn/>
                </immobilie>
              </anbieter>
            </openimmo>
            """;

        var result = Parse(xml);

        result.Listings.Should().BeEmpty();
        result.SkippedCount.Should().Be(1);
    }

    [Test]
    public void Parse_LangeBeschreibung_WirdAuf8000Gekappt()
    {
        var longText = new string('x', 9000);
        var xml = $"""
            <openimmo>
              <uebertragung umfang="VOLL"/>
              <anbieter>
                <immobilie>
                  <objektkategorie>
                    <vermarktungsart KAUF="1"/>
                    <objektart><haus/></objektart>
                  </objektkategorie>
                  <geo><plz>4600</plz><ort>Wels</ort></geo>
                  <preise><kaufpreis>100000</kaufpreis></preise>
                  <freitexte><objektbeschreibung>{longText}</objektbeschreibung></freitexte>
                  <verwaltung_techn><openimmo_obid>LANG-1</openimmo_obid></verwaltung_techn>
                </immobilie>
              </anbieter>
            </openimmo>
            """;

        var listing = Parse(xml).Listings.Single();

        listing.Description.Should().HaveLength(8000);
    }

    [Test]
    public void Parse_DezimalMitKomma_WirdToleriert()
    {
        var xml = """
            <openimmo>
              <uebertragung umfang="VOLL"/>
              <anbieter>
                <immobilie>
                  <objektkategorie>
                    <vermarktungsart KAUF="1"/>
                    <objektart><haus/></objektart>
                  </objektkategorie>
                  <geo><plz>4600</plz><ort>Wels</ort></geo>
                  <preise><kaufpreis>350000,75</kaufpreis></preise>
                  <flaechen><wohnflaeche>120,4</wohnflaeche></flaechen>
                  <verwaltung_techn><openimmo_obid>KOMMA-1</openimmo_obid></verwaltung_techn>
                </immobilie>
              </anbieter>
            </openimmo>
            """;

        var listing = Parse(xml).Listings.Single();

        listing.Price.Should().Be(350000.75m);
        listing.LivingAreaSquareMeters.Should().Be(120);
    }

    [Test]
    public void Parse_KaputtesXml_WirftXmlException()
    {
        var act = () => Parse("<openimmo><anbieter>");

        act.Should().Throw<XmlException>();
    }

    // === Neubauprojekt-Heuristik ===

    /// <summary>Minimal-Haus mit variablen zustand_angaben/verwaltung_objekt-Bloecken.</summary>
    private static string NeubauFixture(string zustandAngaben, string verwaltungObjekt = "", string objektart = "<haus/>")
        => $"""
            <openimmo>
              <uebertragung umfang="VOLL"/>
              <anbieter>
                <immobilie>
                  <objektkategorie>
                    <vermarktungsart KAUF="1"/>
                    <objektart>{objektart}</objektart>
                  </objektkategorie>
                  <geo><plz>4600</plz><ort>Wels</ort></geo>
                  <preise><kaufpreis>450000</kaufpreis></preise>
                  <zustand_angaben>{zustandAngaben}</zustand_angaben>
                  <verwaltung_objekt>{verwaltungObjekt}</verwaltung_objekt>
                  <verwaltung_techn><openimmo_obid>NB-1</openimmo_obid></verwaltung_techn>
                </immobilie>
              </anbieter>
            </openimmo>
            """;

    [Test]
    public void Parse_ErstbezugOhneBaujahr_IstNeubauprojekt()
    {
        var listing = Parse(NeubauFixture("""<zustand zustand_art="ERSTBEZUG"/>""")).Listings.Single();

        listing.IsNewBuildProject.Should().BeTrue("Erstbezug ohne Baujahr = Projekt mit offener Fertigstellung");
    }

    [Test]
    public void Parse_BaujahrInZukunft_IstNeubauprojekt()
    {
        var nextYear = DateTime.UtcNow.Year + 1;
        var listing = Parse(NeubauFixture($"<baujahr>{nextYear}</baujahr>")).Listings.Single();

        listing.IsNewBuildProject.Should().BeTrue("Baujahr in der Zukunft = wird erst gebaut");
    }

    [Test]
    public void Parse_ErstbezugMitVergangenemBaujahr_IstKeinNeubauprojekt()
    {
        var listing = Parse(NeubauFixture(
            """<baujahr>2013</baujahr><zustand zustand_art="ERSTBEZUG"/>""")).Listings.Single();

        listing.IsNewBuildProject.Should().BeFalse(
            "fertiges Haus mit vergangenem Baujahr, auch wenn der Makler ERSTBEZUG setzt");
    }

    [Test]
    public void Parse_Projektiert_IstNeubauprojektTrotzVergangenemBaujahr()
    {
        var listing = Parse(NeubauFixture(
            """<baujahr>2020</baujahr><zustand zustand_art="PROJEKTIERT"/>""")).Listings.Single();

        listing.IsNewBuildProject.Should().BeTrue("PROJEKTIERT/ROHBAU schlagen das Baujahr");
    }

    [Test]
    public void Parse_NeubauSchluesselfertigOhneBaujahr_IstNeubauprojekt()
    {
        var listing = Parse(NeubauFixture(
            """<alter alter_attr="NEUBAU"/>""",
            """<user_defined_simplefield feldname="schluesselfertig">1</user_defined_simplefield>"""))
            .Listings.Single();

        listing.IsNewBuildProject.Should().BeTrue(
            "Justimmo-Bautraegerhaus ohne Baujahr und ohne zustand (Kallham-Fall im Immobaer-Feed)");
    }

    [Test]
    public void Parse_NeubauOhneSchluesselfertig_IstKeinNeubauprojekt()
    {
        var listing = Parse(NeubauFixture("""<alter alter_attr="NEUBAU"/>""")).Listings.Single();

        listing.IsNewBuildProject.Should().BeFalse(
            "alter=NEUBAU allein ist unzuverlaessig (Immobaer flaggt auch Bestand so)");
    }

    [Test]
    public void Parse_GrundstueckMitErstbezug_IstKeinNeubauprojekt()
    {
        var listing = Parse(NeubauFixture(
            """<zustand zustand_art="ERSTBEZUG"/>""",
            objektart: "<grundstueck/>")).Listings.Single();

        listing.IsNewBuildProject.Should().BeFalse("Heuristik gilt nur fuer Haeuser");
    }
}
