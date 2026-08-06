using FluentAssertions;
using Heimatplatz.Api.Features.Dashboards.Services;
using Heimatplatz.Api.Features.Locations.Contracts.Mediator.Requests;
using NUnit.Framework;

namespace Heimatplatz.Api.UnitTests.Features.Dashboards;

[TestFixture]
public class LocationNameResolutionTests
{
    private static readonly Guid VoecklabruckStadtId = Guid.NewGuid();
    private static readonly Guid AtterseeId = Guid.NewGuid();
    private static readonly Guid LinzId = Guid.NewGuid();

    private static GetLocationsResponse BuildHierarchy()
    {
        var districtVb = Guid.NewGuid();
        var districtLinz = Guid.NewGuid();
        var provinceId = Guid.NewGuid();

        return new GetLocationsResponse(
        [
            new FederalProvinceDto(provinceId, "ooe", "Oberösterreich",
            [
                new DistrictDto(districtVb, "voecklabruck", "417", "Vöcklabruck", provinceId,
                [
                    new MunicipalityDto(VoecklabruckStadtId, "voecklabruck", "41745", "Vöcklabruck", "4840", null, districtVb),
                    new MunicipalityDto(AtterseeId, "attersee", "41702", "Attersee am Attersee", "4864", null, districtVb)
                ]),
                new DistrictDto(districtLinz, "linz-stadt", "401", "Linz (Stadt)", provinceId,
                [
                    new MunicipalityDto(LinzId, "linz", "40101", "Linz", "4020", null, districtLinz)
                ])
            ])
        ]);
    }

    [TestCase("Bezirk Vöcklabruck")]
    [TestCase("bezirk voecklabruck")]
    [TestCase("Vöcklabruck")]
    public void ResolveLocationName_DistrictMatchReturnsAllMunicipalities(string input)
    {
        // "Vöcklabruck" ist Bezirk UND Gemeinde - der Bezirkstreffer gewinnt
        // (breiter = naeher an der Suchabsicht)
        var ids = DashboardDefinitionValidator.ResolveLocationName(input, BuildHierarchy());

        ids.Should().BeEquivalentTo([VoecklabruckStadtId, AtterseeId]);
    }

    [TestCase("Attersee am Attersee")]
    [TestCase("attersee am attersee")]
    public void ResolveLocationName_MunicipalityMatchReturnsSingleId(string input)
    {
        var ids = DashboardDefinitionValidator.ResolveLocationName(input, BuildHierarchy());

        ids.Should().Equal(AtterseeId);
    }

    [Test]
    public void ResolveLocationName_UnknownNameReturnsEmpty()
    {
        var ids = DashboardDefinitionValidator.ResolveLocationName("Atlantis", BuildHierarchy());

        ids.Should().BeEmpty();
    }

    [TestCase("Bezirk Vöcklabruck", "voecklabruck")]
    [TestCase("  Gemeinde Weißkirchen ", "weisskirchen")]
    [TestCase("VÖCKLABRUCK", "voecklabruck")]
    public void FoldName_NormalizesUmlautsAndPrefixes(string input, string expected)
    {
        DashboardDefinitionValidator.FoldName(input).Should().Be(expected);
    }
}
