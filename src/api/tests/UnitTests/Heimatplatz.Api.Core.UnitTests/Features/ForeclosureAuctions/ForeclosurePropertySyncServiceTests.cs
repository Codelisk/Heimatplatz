using System.Text.Json;
using FluentAssertions;
using Heimatplatz.Api.Features.ForeclosureAuctions.Data.Entities;
using Heimatplatz.Api.Features.ForeclosureAuctions.Services;
using NUnit.Framework;

namespace Heimatplatz.Api.Core.UnitTests.Features.ForeclosureAuctions;

[TestFixture]
public class ForeclosurePropertySyncServiceTests
{
    [Test]
    public void BuildForeclosurePropertyData_PreservesUtcMarkerInJsonDates()
    {
        var auction = new ForeclosureAuction
        {
            Id = Guid.NewGuid(),
            AuctionDate = new DateTimeOffset(2026, 8, 17, 8, 30, 0, TimeSpan.Zero),
            ViewingDate = new DateTimeOffset(2026, 8, 10, 7, 0, 0, TimeSpan.Zero),
            BiddingDeadline = new DateTimeOffset(2026, 8, 16, 10, 0, 0, TimeSpan.Zero),
            ObjectDescription = "Testobjekt",
            Address = "Teststrasse 1",
            City = "Linz",
            PostalCode = "4020"
        };

        var data = ForeclosurePropertySyncService.BuildForeclosurePropertyData(auction);
        var json = JsonSerializer.Serialize(data);

        data.AuctionDate.Kind.Should().Be(DateTimeKind.Utc);
        data.ViewingDate!.Value.Kind.Should().Be(DateTimeKind.Utc);
        data.BiddingDeadline!.Value.Kind.Should().Be(DateTimeKind.Utc);
        json.Should().Contain("\"AuctionDate\":\"2026-08-17T08:30:00Z\"");
        json.Should().Contain("\"ViewingDate\":\"2026-08-10T07:00:00Z\"");
        json.Should().Contain("\"BiddingDeadline\":\"2026-08-16T10:00:00Z\"");
    }
}
