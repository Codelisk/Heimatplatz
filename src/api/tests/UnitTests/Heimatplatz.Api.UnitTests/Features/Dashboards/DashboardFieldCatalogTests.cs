using FluentAssertions;
using Heimatplatz.Api.Features.Dashboards.Contracts.Models;
using Heimatplatz.Api.Features.Dashboards.Services.Widgets;
using NUnit.Framework;

namespace Heimatplatz.Api.UnitTests.Features.Dashboards;

[TestFixture]
public class DashboardFieldCatalogTests
{
    [Test]
    public void NormalizeFields_DropsUnknownAndContextMismatch()
    {
        var warnings = new List<string>();

        var result = DashboardFieldCatalog.NormalizeFields(
            ["Foto", "titel", "baujahr", "energieklasse", "preis"], forDetail: false, warnings);

        // baujahr ist detail-only, energieklasse unbekannt - beide fliegen im Listen-Kontext
        result.Should().Equal("foto", "titel", "preis");
        warnings.Should().HaveCount(2);
    }

    [Test]
    public void NormalizeFields_DedupesAndCaps()
    {
        var warnings = new List<string>();

        var result = DashboardFieldCatalog.NormalizeFields(
            ["preis", "preis", "titel", "ort", "zimmer", "wohnflaeche", "grundflaeche", "anbieter", "strasse"],
            forDetail: false, warnings);

        result.Should().HaveCount(DashboardFieldCatalog.MaxListFields);
        result.Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void NormalizeFields_EmptyResultBecomesNull()
    {
        var warnings = new List<string>();

        DashboardFieldCatalog.NormalizeFields(["quatsch"], forDetail: false, warnings).Should().BeNull();
        DashboardFieldCatalog.NormalizeFields(null, forDetail: false, warnings).Should().BeNull();
    }

    [Test]
    public void NormalizeFields_DetailContextAllowsDetailOnlyFields()
    {
        var warnings = new List<string>();

        var result = DashboardFieldCatalog.NormalizeFields(
            ["baujahr", "preis-pro-m2", "foto"], forDetail: true, warnings);

        // foto ist list-only und fliegt im Detail-Kontext
        result.Should().Equal("baujahr", "preis-pro-m2");
    }

    [Test]
    public void NormalizeDetail_DropsUnknownSectionsAndCollapsesToNull()
    {
        var warnings = new List<string>();

        var detail = DashboardFieldCatalog.NormalizeDetail(new DashboardDetailSpec
        {
            Sections = ["Gallery", "hologramm", "facts"],
            Fields = ["unbekannt"]
        }, warnings);

        detail.Should().NotBeNull();
        detail!.Sections.Should().Equal("gallery", "facts");
        detail.Fields.Should().BeNull();

        var empty = DashboardFieldCatalog.NormalizeDetail(new DashboardDetailSpec
        {
            Sections = ["hologramm"],
            Fields = ["unbekannt"]
        }, warnings);
        empty.Should().BeNull("komplett Unbrauchbares faellt auf den Standard-Aufbau zurueck");
    }
}
