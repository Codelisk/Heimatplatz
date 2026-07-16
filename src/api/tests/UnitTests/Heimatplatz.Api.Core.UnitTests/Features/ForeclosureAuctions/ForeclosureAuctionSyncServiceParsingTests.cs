using System.Globalization;
using FluentAssertions;
using Heimatplatz.Api.Features.ForeclosureAuctions.Services;
using NUnit.Framework;

namespace Heimatplatz.Api.Core.UnitTests.Features.ForeclosureAuctions;

/// <summary>
/// Termin-Parsing und Edikt-Typ-Erkennung entscheiden gemeinsam, ob eine Liegenschaft
/// als aktive Versteigerung gefuehrt wird. Beide Fehlerklassen sind live aufgetreten:
/// einstellige Stunden ("um 9:00 Uhr") liessen echte Versteigerungen aus dem Bestand
/// fallen, und "Entfall des Termins"-Edikte (fuehren den entfallenen Termin weiterhin
/// unter "Termin:") blieben faelschlich als aktive Inserate online.
/// </summary>
[TestFixture]
public class ForeclosureAuctionSyncServiceParsingTests
{
    // Wiener Ortszeit -> UTC: Sommer = UTC+2, Winter = UTC+1
    [TestCase("am 17.8.2026 um 9:00 Uhr", "2026-08-17T07:00", TestName = "Einstellige Stunde (live: EFH Steyr)")]
    [TestCase("am 16.07.2026 um 11:00 Uhr", "2026-07-16T09:00", TestName = "Zweistellig, Sommerzeit")]
    [TestCase("am 27.2.2026 um 11:00 Uhr", "2026-02-27T10:00", TestName = "Zweistellig, Winterzeit")]
    [TestCase("am 6.8.2026 um 08:30 Uhr", "2026-08-06T06:30", TestName = "Einstelliger Tag, fuehrende Null bei Stunde")]
    public void ParseAuctionDate_ConvertsViennaLocalTimeToUtc(string text, string expectedUtc)
    {
        var parsed = ForeclosureAuctionSyncService.ParseAuctionDate(text);

        parsed.Should().NotBeNull();
        parsed!.Value.Offset.Should().Be(TimeSpan.Zero);
        parsed.Value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture)
            .Should().Be(expectedUtc);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("wird noch bekannt gegeben")]
    [TestCase("am 17.8.2026")] // Datum ohne Uhrzeit
    public void ParseAuctionDate_ReturnsNullWithoutParsableDate(string? text)
    {
        ForeclosureAuctionSyncService.ParseAuctionDate(text).Should().BeNull();
    }

    [TestCase("Entfall des Termins - Objekt 1", true)]
    [TestCase("Zuschlag mit Überbot - Einfamilienhaus", true)]
    [TestCase("Zuschlag ohne Überbot - Wohnung Wels Pernau", true)]
    [TestCase("Meistbotsverteilung", true)]
    [TestCase("Einstellung der Versteigerung", true)]
    [TestCase("Versteigerung - Wohnhaus mit Garage", false)]
    [TestCase("Versteigerung - Objekt 1", false)]
    [TestCase("Verschiebung", false)] // Verschobene Versteigerung hat "Neuer Versteigerungstermin"
    [TestCase(null, false)]
    [TestCase("", false)]
    public void IsConcludedEdictType_DetectsTerminalEdictTypes(string? title, bool expected)
    {
        ForeclosureAuctionSyncService.IsConcludedEdictType(title).Should().Be(expected);
    }
}
