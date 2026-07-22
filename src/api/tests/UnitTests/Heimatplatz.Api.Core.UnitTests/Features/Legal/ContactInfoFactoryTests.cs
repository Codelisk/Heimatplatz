using FluentAssertions;
using Heimatplatz.Api.Features.Legal.Contracts.Models;
using Heimatplatz.Api.Features.Legal.Services;
using NUnit.Framework;

namespace Heimatplatz.Api.Core.UnitTests.Features.Legal;

/// <summary>
/// Die Fallback-Kette Impressum -> Contact-Overrides ist der Kern der Kontakt-Stammdaten:
/// Firma/Adresse kommen immer aus dem Impressum (eine Quelle fuer die Pflichtangaben),
/// E-Mail/Telefon/Website duerfen ueberschrieben werden.
/// </summary>
[TestFixture]
public class ContactInfoFactoryTests
{
    private static ImprintPartyDto Imprint(string email = "info@heimatplatz.at", string? phone = "+43 664 73221804")
        => new(
            CompanyName: "Ing. Daniel Hufnagl",
            LegalForm: "Einzelunternehmen",
            Owner: "Ing. Daniel Hufnagl",
            Street: "Stockham 44",
            PostalCode: "4663",
            City: "Laakirchen",
            Country: "Österreich",
            Email: email,
            Phone: phone,
            Website: "https://www.heimatplatz.at",
            UidNumber: "ATU75151817",
            TaxNumber: "532163383",
            DunsNumber: null,
            Gln: null,
            GisaNumber: null,
            Trade: "IT",
            TradeAuthority: "BH Gmunden",
            ProfessionalLaw: "GewO 1994",
            ChamberMembership: null,
            TradeGroup: null);

    [Test]
    public void Create_UsesImprintValues_WhenNoContactSettingsExist()
    {
        var contact = ContactInfoFactory.Create(Imprint(), contact: null);

        contact.Email.Should().Be("info@heimatplatz.at");
        contact.SupportEmail.Should().Be("info@heimatplatz.at");
        contact.Phone.Should().Be("+43 664 73221804");
        contact.PhoneLink.Should().Be("+4366473221804");
        contact.CompanyName.Should().Be("Ing. Daniel Hufnagl");
        contact.SocialLinks.Should().BeEmpty();
    }

    [Test]
    public void Create_PrefersContactOverrides_OverImprint()
    {
        var settings = new ContactSettingsDto(
            Email: "office@heimatplatz.at",
            SupportEmail: "hilfe@heimatplatz.at",
            Phone: "+43 732 1234567",
            Website: "https://heimatplatz.at",
            OfficeHours: "Mo-Fr 9-17 Uhr");

        var contact = ContactInfoFactory.Create(Imprint(), settings);

        contact.Email.Should().Be("office@heimatplatz.at");
        contact.SupportEmail.Should().Be("hilfe@heimatplatz.at");
        contact.Phone.Should().Be("+43 732 1234567");
        contact.PhoneLink.Should().Be("+437321234567");
        contact.OfficeHours.Should().Be("Mo-Fr 9-17 Uhr");
    }

    [Test]
    public void Create_TreatsBlankOverridesAsUnset()
    {
        // Das Intern-Formular schickt geleerte Felder als "" - das muss "Impressum verwenden"
        // heissen und nicht die Angabe loeschen
        var settings = new ContactSettingsDto(Email: "  ", Phone: "", Website: "   ");

        var contact = ContactInfoFactory.Create(Imprint(), settings);

        contact.Email.Should().Be("info@heimatplatz.at");
        contact.Phone.Should().Be("+43 664 73221804");
        contact.Website.Should().Be("https://www.heimatplatz.at");
    }

    [Test]
    public void Create_FallsBackToGeneralEmail_WhenSupportEmailMissing()
    {
        var settings = new ContactSettingsDto(Email: "office@heimatplatz.at");

        var contact = ContactInfoFactory.Create(Imprint(), settings);

        // SupportEmail ist die Adresse, die Footer und Makler-Seite verlinken - nie leer
        contact.SupportEmail.Should().Be("office@heimatplatz.at");
    }

    [Test]
    public void Create_OmitsPhoneLink_WhenNoPhoneAnywhere()
    {
        var contact = ContactInfoFactory.Create(Imprint(phone: null), contact: null);

        contact.Phone.Should().BeNull();
        contact.PhoneLink.Should().BeNull();
    }

    [Test]
    public void Create_DropsIncompleteSocialLinks()
    {
        var settings = new ContactSettingsDto(SocialLinks:
        [
            new SocialLinkDto("Facebook", "https://facebook.com/heimatplatz"),
            new SocialLinkDto("Instagram", "   "),
            new SocialLinkDto("", "https://example.at")
        ]);

        var contact = ContactInfoFactory.Create(Imprint(), settings);

        contact.SocialLinks.Should().ContainSingle()
            .Which.Platform.Should().Be("Facebook");
    }
}
