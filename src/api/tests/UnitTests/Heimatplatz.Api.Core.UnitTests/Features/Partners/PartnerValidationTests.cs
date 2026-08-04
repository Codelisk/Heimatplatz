using FluentAssertions;
using Heimatplatz.Api.Features.Partners.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Partners.Contracts.Models;
using Heimatplatz.Api.Features.Partners.Services;
using NUnit.Framework;

namespace Heimatplatz.Api.Core.UnitTests.Features.Partners;

/// <summary>
/// Die Save-Validierung ist die einzige Huerde zwischen Intern-Formular und Datenbank -
/// Kategorie-Tippfehler oder javascript:-URLs duerfen nie auf der oeffentlichen
/// Partner-Seite landen.
/// </summary>
[TestFixture]
public class PartnerValidationTests
{
    private static SavePartnerRequest Request(
        string name = "Immobär Immobilien",
        string category = PartnerCategories.Broker,
        string? websiteUrl = "https://www.immobaer.at",
        string? logoUrl = null,
        int? partnerSinceYear = 2026)
        => new(
            Id: null,
            Name: name,
            Category: category,
            Description: "Regionales Maklerbüro im Innviertel.",
            WebsiteUrl: websiteUrl,
            LogoUrl: logoUrl,
            Region: "Innviertel, Oberösterreich",
            PartnerSinceYear: partnerSinceYear,
            SourceName: "immobaer.at",
            SellerName: "Immobär Immobilien",
            DisplayOrder: 10,
            IsVisible: true);

    [Test]
    public void Validate_AcceptsCompleteRequest()
    {
        PartnerValidation.Validate(Request()).Should().BeNull();
    }

    [Test]
    public void Validate_AcceptsEmptyOptionalFields()
    {
        var request = Request(websiteUrl: null, logoUrl: null, partnerSinceYear: null);

        PartnerValidation.Validate(request).Should().BeNull();
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Validate_RejectsMissingName(string name)
    {
        PartnerValidation.Validate(Request(name: name)).Should().Contain("Name");
    }

    [Test]
    public void Validate_RejectsUnknownCategory()
    {
        PartnerValidation.Validate(Request(category: "Sponsor")).Should().Contain("Kategorie");
    }

    [TestCase("www.immobaer.at")]
    [TestCase("ftp://immobaer.at")]
    [TestCase("javascript:alert(1)")]
    public void Validate_RejectsNonHttpWebsite(string url)
    {
        PartnerValidation.Validate(Request(websiteUrl: url)).Should().Contain("http");
    }

    [Test]
    public void Validate_AcceptsUploadedLogoPath()
    {
        var request = Request(logoUrl: "/uploads/properties/abc.display.jpg");

        PartnerValidation.Validate(request).Should().BeNull();
    }

    [Test]
    public void Validate_AcceptsAbsoluteLogoUrl()
    {
        var request = Request(logoUrl: "https://api.heimatplatz.at/uploads/properties/abc.display.jpg");

        PartnerValidation.Validate(request).Should().BeNull();
    }

    [Test]
    public void Validate_RejectsLogoOutsideUploadsWithoutScheme()
    {
        PartnerValidation.Validate(Request(logoUrl: "uploads/x.jpg")).Should().NotBeNull();
    }

    [TestCase(1899)]
    [TestCase(2101)]
    public void Validate_RejectsImplausiblePartnerSinceYear(int year)
    {
        PartnerValidation.Validate(Request(partnerSinceYear: year)).Should().Contain("Jahr");
    }
}
