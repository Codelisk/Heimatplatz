using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Core.Data.Seeding;
using Heimatplatz.Api.Features.ForeclosureAuctions.Contracts;
using Heimatplatz.Api.Features.ForeclosureAuctions.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Heimatplatz.Api.Features.ForeclosureAuctions.Data.Seeding;

public class ForeclosureAuctionSeeder(AppDbContext dbContext) : ISeeder
{
    public int Order => 20;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Idempotent: Nur seeden wenn leer
        if (await dbContext.Set<ForeclosureAuction>().AnyAsync(cancellationToken))
            return;

        var baseDate = DateTimeOffset.UtcNow;

        var auctions = new List<ForeclosureAuction>
        {
            // Wien
            new ForeclosureAuction
            {
                AuctionDate = baseDate.AddDays(30),
                Address = "Mariahilfer Straße 123",
                City = "Wien",
                PostalCode = "1060",
                State = AustrianState.Wien,
                Category = PropertyCategory.Wohnungseigentum,
                ObjectDescription = "3-Zimmer-Wohnung mit Balkon in zentraler Lage",
                EstimatedValue = 350000m,
                MinimumBid = 280000m,
                CaseNumber = "123 E 456/24",
                Court = "Bezirksgericht Innere Stadt Wien",
                EdictUrl = "https://edikte.justiz.gv.at/sample1.odt",
                Notes = "Sanierungsbedürftig, gute Anbindung"
            },
            new ForeclosureAuction
            {
                AuctionDate = baseDate.AddDays(45),
                Address = "Praterstraße 45",
                City = "Wien",
                PostalCode = "1020",
                State = AustrianState.Wien,
                Category = PropertyCategory.GewerblicheLiegenschaft,
                ObjectDescription = "Geschäftslokal mit Lager im Erdgeschoss",
                EstimatedValue = 520000m,
                MinimumBid = 416000m,
                CaseNumber = "234 E 567/24",
                Court = "Bezirksgericht Leopoldstadt",
                EdictUrl = "https://edikte.justiz.gv.at/sample2.odt"
            },

            // Niederösterreich
            new ForeclosureAuction
            {
                AuctionDate = baseDate.AddDays(21),
                Address = "Hauptstraße 78",
                City = "Baden",
                PostalCode = "2500",
                State = AustrianState.Niederoesterreich,
                Category = PropertyCategory.Einfamilienhaus,
                ObjectDescription = "Einfamilienhaus mit Garten und Garage",
                EstimatedValue = 480000m,
                MinimumBid = 384000m,
                CaseNumber = "345 E 678/24",
                Court = "Bezirksgericht Baden",
                Notes = "Baujahr 1985, teilweise renoviert"
            },
            new ForeclosureAuction
            {
                AuctionDate = baseDate.AddDays(60),
                Address = "Waldweg 12",
                City = "Mödling",
                PostalCode = "2340",
                State = AustrianState.Niederoesterreich,
                Category = PropertyCategory.Grundstueck,
                ObjectDescription = "Baugrundstück in ruhiger Siedlungslage",
                EstimatedValue = 180000m,
                MinimumBid = 144000m,
                CaseNumber = "456 E 789/24",
                Court = "Bezirksgericht Mödling"
            },

            // Oberösterreich
            new ForeclosureAuction
            {
                AuctionDate = baseDate.AddDays(35),
                Address = "Landstraße 234",
                City = "Linz",
                PostalCode = "4020",
                State = AustrianState.Oberoesterreich,
                Category = PropertyCategory.Mehrfamilienhaus,
                ObjectDescription = "Mehrfamilienhaus mit 6 Wohneinheiten",
                EstimatedValue = 890000m,
                MinimumBid = 712000m,
                CaseNumber = "567 E 890/24",
                Court = "Bezirksgericht Linz",
                Notes = "Vollvermietet, guter Zustand"
            },
            new ForeclosureAuction
            {
                AuctionDate = baseDate.AddDays(50),
                Address = "Industriestraße 56",
                City = "Wels",
                PostalCode = "4600",
                State = AustrianState.Oberoesterreich,
                Category = PropertyCategory.GewerblicheLiegenschaft,
                ObjectDescription = "Lagerhalle mit Bürotrakt",
                EstimatedValue = 650000m,
                MinimumBid = 520000m,
                CaseNumber = "678 E 901/24",
                Court = "Bezirksgericht Wels"
            },

            // Steiermark
            new ForeclosureAuction
            {
                AuctionDate = baseDate.AddDays(28),
                Address = "Herrengasse 89",
                City = "Graz",
                PostalCode = "8010",
                State = AustrianState.Steiermark,
                Category = PropertyCategory.Zweifamilienhaus,
                ObjectDescription = "Zweifamilienhaus mit zwei separaten Eingängen",
                EstimatedValue = 520000m,
                MinimumBid = 416000m,
                CaseNumber = "789 E 012/24",
                Court = "Bezirksgericht für Zivilrechtssachen Graz"
            },

            // Tirol
            new ForeclosureAuction
            {
                AuctionDate = baseDate.AddDays(42),
                Address = "Alpweg 34",
                City = "Innsbruck",
                PostalCode = "6020",
                State = AustrianState.Tirol,
                Category = PropertyCategory.Einfamilienhaus,
                ObjectDescription = "Chalet-artiges Einfamilienhaus mit Bergblick",
                EstimatedValue = 720000m,
                MinimumBid = 576000m,
                CaseNumber = "890 E 123/24",
                Court = "Bezirksgericht Innsbruck",
                Notes = "Touristische Vermietung möglich"
            },

            // Salzburg
            new ForeclosureAuction
            {
                AuctionDate = baseDate.AddDays(25),
                Address = "Getreidegasse 67",
                City = "Salzburg",
                PostalCode = "5020",
                State = AustrianState.Salzburg,
                Category = PropertyCategory.Wohnungseigentum,
                ObjectDescription = "Altstadt-Wohnung mit historischem Charme",
                EstimatedValue = 420000m,
                MinimumBid = 336000m,
                CaseNumber = "901 E 234/24",
                Court = "Bezirksgericht Salzburg",
                Notes = "Denkmalschutz zu beachten"
            },

            // Kärnten
            new ForeclosureAuction
            {
                AuctionDate = baseDate.AddDays(55),
                Address = "Seestraße 15",
                City = "Klagenfurt",
                PostalCode = "9020",
                State = AustrianState.Kaernten,
                Category = PropertyCategory.Einfamilienhaus,
                ObjectDescription = "Einfamilienhaus mit Seeblick und Bootssteg",
                EstimatedValue = 680000m,
                MinimumBid = 544000m,
                CaseNumber = "012 E 345/24",
                Court = "Bezirksgericht Klagenfurt"
            },

            // Vorarlberg
            new ForeclosureAuction
            {
                AuctionDate = baseDate.AddDays(38),
                Address = "Bergstraße 88",
                City = "Bregenz",
                PostalCode = "6900",
                State = AustrianState.Vorarlberg,
                Category = PropertyCategory.Wohnungseigentum,
                ObjectDescription = "Penthouse-Wohnung mit Bodenseeblick",
                EstimatedValue = 580000m,
                MinimumBid = 464000m,
                CaseNumber = "123 E 456/24",
                Court = "Bezirksgericht Bregenz"
            },

            // Burgenland
            new ForeclosureAuction
            {
                AuctionDate = baseDate.AddDays(32),
                Address = "Weingartenweg 22",
                City = "Eisenstadt",
                PostalCode = "7000",
                State = AustrianState.Burgenland,
                Category = PropertyCategory.LandUndForstwirtschaft,
                ObjectDescription = "Weingut mit Wohnhaus und Produktionshallen",
                EstimatedValue = 950000m,
                MinimumBid = 760000m,
                CaseNumber = "234 E 567/24",
                Court = "Bezirksgericht Eisenstadt",
                Notes = "Inklusive Weinreben und Maschinen"
            }
        };

        dbContext.Set<ForeclosureAuction>().AddRange(auctions);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
