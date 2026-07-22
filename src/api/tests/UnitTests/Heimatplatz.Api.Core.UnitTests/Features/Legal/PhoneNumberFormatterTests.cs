using FluentAssertions;
using Heimatplatz.Api.Features.Legal.Services;
using NUnit.Framework;

namespace Heimatplatz.Api.Core.UnitTests.Features.Legal;

/// <summary>
/// Der tel:-Link wird serverseitig gebaut, damit die Frontends die Nummer nicht jeweils
/// selbst normalisieren (und dabei auseinanderlaufen).
/// </summary>
[TestFixture]
public class PhoneNumberFormatterTests
{
    [TestCase("+43 664 73221804", "+4366473221804")]
    [TestCase("+43 (0)664 732-218-04", "+4366473221804")]
    [TestCase("0043 664 73221804", "+4366473221804")]
    [TestCase("+436647322180", "+436647322180")]
    public void ToTelLink_NormalizesInternationalFormats(string input, string expected)
    {
        PhoneNumberFormatter.ToTelLink(input).Should().Be(expected);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("keine Nummer")]
    public void ToTelLink_ReturnsNull_WhenNothingDialable(string? input)
    {
        // Leerer Rueckgabewert statt "tel:" - die Frontends blenden die Zeile dann aus
        PhoneNumberFormatter.ToTelLink(input).Should().BeNull();
    }

    [Test]
    public void ToTelLink_KeepsNationalNumberUnchanged_WhenNoCountryCodeGiven()
    {
        // Bewusst KEIN Erraten von +43: eine falsch geratene Vorwahl waehlt beim Nutzer
        // eine fremde Nummer. Gepflegt wird deshalb immer international.
        PhoneNumberFormatter.ToTelLink("0664 73221804").Should().Be("066473221804");
    }
}
