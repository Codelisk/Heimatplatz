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
            // Oberösterreich - Linz
            new ForeclosureAuction
            {
                AuctionDate = baseDate.AddDays(35),
                Category = PropertyCategory.Mehrfamilienhaus,
                ObjectDescription = "Mehrfamilienhaus mit 6 Wohneinheiten",
                Status = "Aktiv",
                Address = "Landstraße 234",
                City = "Linz",
                PostalCode = "4020",
                RegistrationNumber = "EZ 1234/2024",
                CadastralMunicipality = "Linz Stadt",
                PlotNumber = "567/8",
                SheetNumber = "Blatt 12",
                TotalArea = 850m,
                BuildingArea = 620m,
                PlotArea = 850m,
                YearBuilt = 1998,
                NumberOfRooms = 24,
                ZoningDesignation = "Bauland-Wohngebiet",
                BuildingCondition = "Gut, vollvermietet",
                EstimatedValue = 890000m,
                MinimumBid = 712000m,
                ViewingDate = baseDate.AddDays(28),
                CaseNumber = "567 E 890/24",
                Court = "Bezirksgericht Linz",
                Notes = "Vollvermietet, guter Zustand, zentrale Lage"
            },

            // Oberösterreich - Wels
            new ForeclosureAuction
            {
                AuctionDate = baseDate.AddDays(50),
                Category = PropertyCategory.GewerblicheLiegenschaft,
                ObjectDescription = "Lagerhalle mit Bürotrakt",
                Status = "Aktiv",
                Address = "Industriestraße 56",
                City = "Wels",
                PostalCode = "4600",
                RegistrationNumber = "EZ 5678/2024",
                CadastralMunicipality = "Wels Industriegebiet",
                PlotNumber = "234/1",
                SheetNumber = "Blatt 45",
                TotalArea = 2400m,
                BuildingArea = 1800m,
                PlotArea = 2400m,
                YearBuilt = 2005,
                ZoningDesignation = "Betriebsbaugebiet",
                BuildingCondition = "Sehr gut, betriebsbereit",
                EstimatedValue = 650000m,
                MinimumBid = 520000m,
                ViewingDate = baseDate.AddDays(43),
                BiddingDeadline = baseDate.AddDays(49),
                CaseNumber = "678 E 901/24",
                Court = "Bezirksgericht Wels",
                Notes = "Moderne Lagerhalle mit vollausgestattetem Bürobereich"
            },

            // Oberösterreich - Steyr
            new ForeclosureAuction
            {
                AuctionDate = baseDate.AddDays(28),
                Category = PropertyCategory.Einfamilienhaus,
                ObjectDescription = "Einfamilienhaus mit Garage und großem Garten",
                Status = "Aktiv",
                Address = "Resthofstraße 45",
                City = "Steyr",
                PostalCode = "4400",
                RegistrationNumber = "EZ 3456/2024",
                CadastralMunicipality = "Steyr Münichholz",
                PlotNumber = "789/12",
                SheetNumber = "Blatt 23",
                TotalArea = 650m,
                BuildingArea = 180m,
                GardenArea = 450m,
                PlotArea = 650m,
                YearBuilt = 1985,
                NumberOfRooms = 6,
                ZoningDesignation = "Bauland-Wohngebiet",
                BuildingCondition = "Renovierungsbedürftig",
                EstimatedValue = 420000m,
                MinimumBid = 336000m,
                ViewingDate = baseDate.AddDays(21),
                CaseNumber = "234 E 123/24",
                Court = "Bezirksgericht Steyr",
                Notes = "Ruhige Lage, großer Garten, Renovierungspotential"
            },

            // Oberösterreich - Vöcklabruck
            new ForeclosureAuction
            {
                AuctionDate = baseDate.AddDays(42),
                Category = PropertyCategory.Zweifamilienhaus,
                ObjectDescription = "Zweifamilienhaus mit separaten Eingängen",
                Status = "Aktiv",
                Address = "Salzburger Straße 89",
                City = "Vöcklabruck",
                PostalCode = "4840",
                RegistrationNumber = "EZ 7890/2024",
                CadastralMunicipality = "Vöcklabruck",
                PlotNumber = "456/3",
                SheetNumber = "Blatt 67",
                TotalArea = 520m,
                BuildingArea = 240m,
                GardenArea = 280m,
                PlotArea = 520m,
                YearBuilt = 1992,
                NumberOfRooms = 10,
                ZoningDesignation = "Bauland-Wohngebiet",
                BuildingCondition = "Gut erhalten, teilweise modernisiert",
                EstimatedValue = 495000m,
                MinimumBid = 396000m,
                ViewingDate = baseDate.AddDays(35),
                CaseNumber = "890 E 456/24",
                Court = "Bezirksgericht Vöcklabruck",
                Notes = "Zwei separate Wohneinheiten, ideal für Vermietung"
            },

            // Oberösterreich - Braunau am Inn
            new ForeclosureAuction
            {
                AuctionDate = baseDate.AddDays(60),
                Category = PropertyCategory.Wohnungseigentum,
                ObjectDescription = "3-Zimmer-Wohnung mit Balkon und Tiefgaragenplatz",
                Status = "Aktiv",
                Address = "Stadtplatz 12",
                City = "Braunau am Inn",
                PostalCode = "5280",
                RegistrationNumber = "EZ 2345/2024",
                CadastralMunicipality = "Braunau am Inn",
                PlotNumber = "123/45/6",
                SheetNumber = "Blatt 8, Top 6",
                TotalArea = 95m,
                BuildingArea = 95m,
                YearBuilt = 2010,
                NumberOfRooms = 3,
                ZoningDesignation = "Bauland-Kerngebiet",
                BuildingCondition = "Sehr gut, neuwertig",
                EstimatedValue = 285000m,
                MinimumBid = 228000m,
                ViewingDate = baseDate.AddDays(53),
                OwnershipShare = "1/1",
                CaseNumber = "123 E 789/24",
                Court = "Bezirksgericht Braunau am Inn",
                EdictUrl = "https://edikte.justiz.gv.at/sample-braunau.odt",
                Notes = "Zentrale Lage, moderne Ausstattung, inkl. Tiefgaragenplatz"
            },

            // Oberösterreich - Ried im Innkreis
            new ForeclosureAuction
            {
                AuctionDate = baseDate.AddDays(38),
                Category = PropertyCategory.GewerblicheLiegenschaft,
                ObjectDescription = "Geschäftslokal mit Schaufensterfront in bester Innenstadtlage",
                Status = "Aktiv",
                Address = "Hauptplatz 5",
                City = "Ried im Innkreis",
                PostalCode = "4910",
                RegistrationNumber = "EZ 6789/2024",
                CadastralMunicipality = "Ried im Innkreis",
                PlotNumber = "234/7",
                SheetNumber = "Blatt 34",
                TotalArea = 140m,
                BuildingArea = 140m,
                YearBuilt = 1975,
                ZoningDesignation = "Bauland-Kerngebiet",
                BuildingCondition = "Saniert, betriebsfertig",
                EstimatedValue = 320000m,
                MinimumBid = 256000m,
                ViewingDate = baseDate.AddDays(31),
                CaseNumber = "456 E 234/24",
                Court = "Bezirksgericht Ried im Innkreis",
                Notes = "Beste Lage am Hauptplatz, hohe Frequenz"
            },

            // Oberösterreich - Gmunden (Salzkammergut)
            new ForeclosureAuction
            {
                AuctionDate = baseDate.AddDays(55),
                Category = PropertyCategory.Einfamilienhaus,
                ObjectDescription = "Einfamilienhaus mit Seeblick am Traunsee",
                Status = "Aktiv",
                Address = "Seestraße 78",
                City = "Gmunden",
                PostalCode = "4810",
                RegistrationNumber = "EZ 4567/2024",
                CadastralMunicipality = "Gmunden",
                PlotNumber = "890/4",
                SheetNumber = "Blatt 56",
                TotalArea = 780m,
                BuildingArea = 220m,
                GardenArea = 560m,
                PlotArea = 780m,
                YearBuilt = 2002,
                NumberOfRooms = 7,
                ZoningDesignation = "Bauland-Wohngebiet See",
                BuildingCondition = "Sehr gut, hochwertige Ausstattung",
                EstimatedValue = 875000m,
                MinimumBid = 700000m,
                ViewingDate = baseDate.AddDays(48),
                BiddingDeadline = baseDate.AddDays(54),
                CaseNumber = "789 E 567/24",
                Court = "Bezirksgericht Gmunden",
                EdictUrl = "https://edikte.justiz.gv.at/sample-gmunden.odt",
                Notes = "Traumhafte Seelage, direkter Seezugang, hochwertige Ausstattung"
            },

            // Oberösterreich - Kirchdorf an der Krems
            new ForeclosureAuction
            {
                AuctionDate = baseDate.AddDays(45),
                Category = PropertyCategory.LandUndForstwirtschaft,
                ObjectDescription = "Landwirtschaftlicher Betrieb mit Wohnhaus und Nebengebäuden",
                Status = "Aktiv",
                Address = "Kremstalstraße 156",
                City = "Kirchdorf an der Krems",
                PostalCode = "4560",
                RegistrationNumber = "EZ 8901/2024",
                CadastralMunicipality = "Kirchdorf an der Krems",
                PlotNumber = "567/1",
                SheetNumber = "Blatt 89",
                TotalArea = 25000m,
                BuildingArea = 450m,
                PlotArea = 25000m,
                YearBuilt = 1978,
                NumberOfRooms = 8,
                ZoningDesignation = "Grünland-Land- und Forstwirtschaft",
                BuildingCondition = "Funktionsfähig, Modernisierungsbedarf",
                EstimatedValue = 680000m,
                MinimumBid = 544000m,
                ViewingDate = baseDate.AddDays(38),
                CaseNumber = "901 E 890/24",
                Court = "Bezirksgericht Kirchdorf an der Krems",
                Notes = "Ca. 2,5 ha Grund, Wohnhaus, Stallungen, Scheunen"
            }
        };

        dbContext.Set<ForeclosureAuction>().AddRange(auctions);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
