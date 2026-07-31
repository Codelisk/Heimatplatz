using FluentAssertions;
using Heimatplatz.Api.Features.Marketing.Services;
using Heimatplatz.Api.UnitTests.Infrastructure;
using NUnit.Framework;

namespace Heimatplatz.Api.Core.UnitTests.Features.Marketing;

/// <summary>
/// Domain-Fallback der Posteingang-Zuordnung: Absender-Domains duerfen nur dann einem
/// Kontakt zugeordnet werden, wenn die Domain eindeutig zu genau einem Kontakt gehoert
/// und kein oeffentlicher Mail-Provider ist - nie raten.
/// </summary>
[TestFixture]
[Category(TestCategories.Unit)]
[Category(TestCategories.Fast)]
public class MarketingInboundMatchingTests : BaseApiUnitTest
{
    private static readonly string[] PublicDomains = ["gmail.com", "gmx.at"];

    [TestCase("office@immobaer.at", "immobaer.at")]
    [TestCase("Christoph.Blank@IMMOBAER.AT", "immobaer.at")]
    [TestCase("  x@sub.firma.co.at ", "sub.firma.co.at")]
    [TestCase("kein-at-zeichen", null)]
    [TestCase("endet-mit@", null)]
    [TestCase("domain-ohne-punkt@localhost", null)]
    [TestCase("", null)]
    [TestCase(null, null)]
    public void ExtractDomain_Cases(string? address, string? expected)
    {
        MarketingInboundMatching.ExtractDomain(address).Should().Be(expected);
    }

    [Test]
    public void TryResolveByDomain_UniqueCompanyDomain_MatchesContact()
    {
        var immobaer = Guid.NewGuid();
        var index = MarketingInboundMatching.BuildDomainIndex(
            [new("office@immobaer.at", immobaer)], PublicDomains);

        MarketingInboundMatching.TryResolveByDomain("christoph.blank@immobaer.at", index, out var contactId)
            .Should().BeTrue();
        contactId.Should().Be(immobaer);
    }

    [Test]
    public void TryResolveByDomain_PublicProviderDomain_NeverMatches()
    {
        var contact = Guid.NewGuid();
        var index = MarketingInboundMatching.BuildDomainIndex(
            [new("privatverkauf@gmail.com", contact)], PublicDomains);

        // Auch wenn nur EIN Kontakt eine gmail-Adresse hat: gmail sagt nichts ueber die Firma
        MarketingInboundMatching.TryResolveByDomain("fremder@gmail.com", index, out _)
            .Should().BeFalse();
    }

    [Test]
    public void TryResolveByDomain_DomainSharedByTwoContacts_IsAmbiguous()
    {
        var index = MarketingInboundMatching.BuildDomainIndex(
            [
                new("bh@findmyhome.at", Guid.NewGuid()),
                new("ks@findmyhome.at", Guid.NewGuid())
            ],
            PublicDomains);

        MarketingInboundMatching.TryResolveByDomain("neu@findmyhome.at", index, out _)
            .Should().BeFalse();
    }

    [Test]
    public void TryResolveByDomain_MultipleAddressesOfSameContact_StaysUnique()
    {
        var contact = Guid.NewGuid();
        var index = MarketingInboundMatching.BuildDomainIndex(
            [
                new("office@immobaer.at", contact),
                new("christoph.blank@immobaer.at", contact)
            ],
            PublicDomains);

        MarketingInboundMatching.TryResolveByDomain("neu@immobaer.at", index, out var contactId)
            .Should().BeTrue();
        contactId.Should().Be(contact);
    }

    [Test]
    public void TryResolveByDomain_UnknownDomain_DoesNotMatch()
    {
        var index = MarketingInboundMatching.BuildDomainIndex(
            [new("office@immobaer.at", Guid.NewGuid())], PublicDomains);

        MarketingInboundMatching.TryResolveByDomain("wer@anderefirma.at", index, out _)
            .Should().BeFalse();
    }

    [Test]
    public void TryResolveByDomain_AddressWithoutDomain_DoesNotMatch()
    {
        var index = MarketingInboundMatching.BuildDomainIndex(
            [new("office@immobaer.at", Guid.NewGuid())], PublicDomains);

        MarketingInboundMatching.TryResolveByDomain("kaputt", index, out _)
            .Should().BeFalse();
    }
}
