using FluentAssertions;
using Heimatplatz.Api.Features.Dashboards.Contracts.Models;
using Heimatplatz.Api.Features.Dashboards.Services.Widgets;
using NUnit.Framework;

namespace Heimatplatz.Api.UnitTests.Features.Dashboards;

[TestFixture]
public class PropertyQueryMapperTests
{
    [Test]
    public void Sanitize_NormalizesGermanAliasesAndDropsUnknown()
    {
        var warnings = new List<string>();
        var query = PropertyQueryMapper.Sanitize(new DashboardPropertyQuery
        {
            Types = ["Haus", "grundstück", "Wohnung"],
            Sellers = ["Makler"]
        }, maxListItems: 24, warnings);

        query.Types.Should().Equal("house", "land");
        query.Sellers.Should().Equal("broker");
        warnings.Should().ContainSingle(w => w.Contains("Wohnung"));
    }

    [Test]
    public void Sanitize_BothSellerKindsMeanNoFilter()
    {
        var warnings = new List<string>();
        var query = PropertyQueryMapper.Sanitize(new DashboardPropertyQuery
        {
            Sellers = ["private", "broker"]
        }, 24, warnings);

        query.Sellers.Should().BeNull();
    }

    [Test]
    public void Sanitize_SwapsInvertedPriceRangeAndDropsNegatives()
    {
        var warnings = new List<string>();
        var query = PropertyQueryMapper.Sanitize(new DashboardPropertyQuery
        {
            PriceMin = 400_000,
            PriceMax = 200_000,
            AreaMin = -5,
            RoomsMin = 0
        }, 24, warnings);

        query.PriceMin.Should().Be(200_000);
        query.PriceMax.Should().Be(400_000);
        query.AreaMin.Should().BeNull();
        query.RoomsMin.Should().BeNull();
    }

    [Test]
    public void Sanitize_RejectsUnknownSortAndClampsLimit()
    {
        var warnings = new List<string>();
        var query = PropertyQueryMapper.Sanitize(new DashboardPropertyQuery
        {
            Sort = "random",
            Limit = 999
        }, maxListItems: 24, warnings);

        query.Sort.Should().BeNull();
        query.Limit.Should().Be(24);
        warnings.Should().ContainSingle(w => w.Contains("random"));
    }

    [Test]
    public void Sanitize_NeverTrustsAiMunicipalityIds()
    {
        var warnings = new List<string>();
        var query = PropertyQueryMapper.Sanitize(new DashboardPropertyQuery
        {
            MunicipalityIds = [Guid.NewGuid()]
        }, 24, warnings);

        query.MunicipalityIds.Should().BeNull();
    }

    [Test]
    public void ToGetPropertiesRequest_DefaultsToHouseAndLandWithoutForeclosure()
    {
        var request = PropertyQueryMapper.ToGetPropertiesRequest(new DashboardPropertyQuery(), pageSize: 6);

        // ZV-Produktregel: default-aus - nur House (1) + Land (2)
        request.PropertyTypesJson.Should().Be("[1,2]");
        request.PageSize.Should().Be(6);
        request.SortBy.Should().Be("CreatedAt");
        request.SortDescending.Should().BeTrue();
    }

    [Test]
    public void ToGetPropertiesRequest_MapsExplicitForeclosureAndPriceSort()
    {
        var request = PropertyQueryMapper.ToGetPropertiesRequest(new DashboardPropertyQuery
        {
            Types = ["foreclosure"],
            Sort = "price-asc"
        }, pageSize: 1);

        request.PropertyTypesJson.Should().Be("[3]");
        request.SortBy.Should().Be("Price");
        request.SortDescending.Should().BeFalse();
    }

    [Test]
    public void ToGetPropertiesRequest_BrokerIncludesPropertyManager()
    {
        var request = PropertyQueryMapper.ToGetPropertiesRequest(new DashboardPropertyQuery
        {
            Sellers = ["broker"]
        }, pageSize: 6);

        // Broker (2) + PropertyManager (3), wie der Web-Anbieter-Filter
        request.SellerTypesJson.Should().Be("[2,3]");
    }

    [Test]
    public void FormatPrice_UsesCanonWithDotSeparator()
    {
        PropertyQueryMapper.FormatPrice(520_000m).Should().Be("€ 520.000");
        PropertyQueryMapper.FormatPrice(950m).Should().Be("€ 950");
    }
}
