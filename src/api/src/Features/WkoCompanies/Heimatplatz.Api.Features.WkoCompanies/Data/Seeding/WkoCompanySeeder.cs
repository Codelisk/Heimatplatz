using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Core.Data.Seeding;
using Heimatplatz.Api.Features.WkoCompanies.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Heimatplatz.Api.Features.WkoCompanies.Data.Seeding;

public class WkoCompanySeeder(AppDbContext dbContext) : ISeeder
{
    public int Order => 21;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Idempotent: Nur seeden wenn leer
        if (await dbContext.Set<WkoCompany>().AnyAsync(cancellationToken))
            return;

        var now = DateTimeOffset.UtcNow;

        var companies = new List<WkoCompany>
        {
            new()
            {
                Name = "Linzer Heimat Immobilien GmbH",
                CategoryText = "Immobilienmakler Immobilienverwalter",
                Street = "Landstraße 112",
                PostalCode = "4020",
                City = "Linz",
                Phones = ["0732 555123"],
                Email = "office@linzer-heimat-immobilien.at",
                Website = "https://www.linzer-heimat-immobilien.at",
                CompanyRegisterNumber = "512345a",
                CompanyCourt = "Landesgericht Linz",
                LegalForm = "GmbH",
                FoundedYear = 2011,
                FoundedDate = new DateTimeOffset(2011, 5, 12, 11, 0, 0, TimeSpan.Zero),
                Permits =
                [
                    new WkoCompanyPermit
                    {
                        FachgruppeName = "FG Immobilien- und Vermögenstreuhänder",
                        Description = "Immobilientreuhänder (Immobilienmakler, Immobilienverwalter, Bauträger), eingeschränkt auf Immobilienmakler",
                        ManagingDirector = "Andrea Steinbrecher",
                        GisaNumber = "20112233",
                        Since = new DateTimeOffset(2011, 5, 12, 11, 0, 0, TimeSpan.Zero)
                    }
                ],
                WkoFirmaId = Guid.Parse("11111111-1111-4111-8111-111111111101"),
                DetailUrl = "https://firmen.wko.at/linzer-heimat-immobilien-gmbh/oberösterreich/?firmaid=11111111-1111-4111-8111-111111111101&standortid=3&standortname=oberösterreich%20(bundesland)&suchbegriff=immobilienmakler",
                SourceSearchTerm = "Immobilienmakler",
                IsActive = true,
                FirstSeenAt = now,
                LastScrapedAt = now
            },
            new()
            {
                Name = "Welser Wohnbau Makler KG",
                CategoryText = "Immobilienmakler",
                Street = "Ringstraße 44",
                PostalCode = "4600",
                City = "Wels",
                Phones = ["07242 66778", "0664 1234567"],
                Email = "kontakt@welser-wohnbau-makler.at",
                Website = "https://www.welser-wohnbau-makler.at",
                CompanyRegisterNumber = "487621f",
                CompanyCourt = "Landesgericht Wels",
                LegalForm = "KG",
                FoundedYear = 2016,
                FoundedDate = new DateTimeOffset(2016, 3, 7, 11, 0, 0, TimeSpan.Zero),
                Permits =
                [
                    new WkoCompanyPermit
                    {
                        FachgruppeName = "FG Immobilien- und Vermögenstreuhänder",
                        Description = "Immobilientreuhänder (Immobilienmakler, Immobilienverwalter, Bauträger), eingeschränkt auf Immobilienmakler",
                        ManagingDirector = "Bernhard Grubinger",
                        GisaNumber = "20334455",
                        Since = new DateTimeOffset(2016, 3, 7, 11, 0, 0, TimeSpan.Zero)
                    }
                ],
                WkoFirmaId = Guid.Parse("11111111-1111-4111-8111-111111111102"),
                DetailUrl = "https://firmen.wko.at/welser-wohnbau-makler-kg/oberösterreich/?firmaid=11111111-1111-4111-8111-111111111102&standortid=3&standortname=oberösterreich%20(bundesland)&suchbegriff=immobilienmakler",
                SourceSearchTerm = "Immobilienmakler",
                IsActive = true,
                FirstSeenAt = now,
                LastScrapedAt = now
            },
            new()
            {
                Name = "Steyrtal Immobilienverwaltung e.U.",
                CategoryText = "Immobilienverwalter",
                Street = "Resthofstraße 9",
                PostalCode = "4400",
                City = "Steyr",
                Phones = ["07252 44990"],
                Email = "verwaltung@steyrtal-immo.at",
                Website = "https://www.steyrtal-immo.at",
                CompanyRegisterNumber = "601122k",
                CompanyCourt = "Landesgericht Steyr",
                LegalForm = "e.U.",
                FoundedYear = 2004,
                FoundedDate = new DateTimeOffset(2004, 9, 20, 10, 0, 0, TimeSpan.Zero),
                OpeningHoursText = "Mo-Do 8:00-16:00 Uhr, Fr 8:00-13:00 Uhr",
                Permits =
                [
                    new WkoCompanyPermit
                    {
                        FachgruppeName = "FG Immobilien- und Vermögenstreuhänder",
                        Description = "Immobilientreuhänder (Immobilienmakler, Immobilienverwalter, Bauträger), eingeschränkt auf Immobilienverwalter",
                        ManagingDirector = "Claudia Feichtinger",
                        GisaNumber = "20556677",
                        Since = new DateTimeOffset(2004, 9, 20, 10, 0, 0, TimeSpan.Zero)
                    }
                ],
                WkoFirmaId = Guid.Parse("11111111-1111-4111-8111-111111111103"),
                DetailUrl = "https://firmen.wko.at/steyrtal-immobilienverwaltung-eu/oberösterreich/?firmaid=11111111-1111-4111-8111-111111111103&standortid=3&standortname=oberösterreich%20(bundesland)&suchbegriff=immobilienverwaltung",
                SourceSearchTerm = "Immobilienverwaltung",
                IsActive = true,
                FirstSeenAt = now,
                LastScrapedAt = now
            },
            new()
            {
                Name = "Salzkammergut Immobilientreuhand GmbH",
                CategoryText = "Immobilientreuhänder, Bauträger, Bewerter",
                Street = "Seestraße 21",
                PostalCode = "4810",
                City = "Gmunden",
                Phones = ["07612 778899"],
                Email = "info@skg-immobilientreuhand.at",
                Website = "https://www.skg-immobilientreuhand.at",
                CompanyRegisterNumber = "398877b",
                CompanyCourt = "Landesgericht Wels",
                LegalForm = "GmbH",
                FoundedYear = 1998,
                FoundedDate = new DateTimeOffset(1998, 4, 2, 10, 0, 0, TimeSpan.Zero),
                Permits =
                [
                    new WkoCompanyPermit
                    {
                        FachgruppeName = "FG Immobilien- und Vermögenstreuhänder",
                        Description = "Immobilientreuhänder (Immobilienmakler, Immobilienverwalter, Bauträger)",
                        ManagingDirector = "Michael Hörzinger",
                        GisaNumber = "20778899",
                        Since = new DateTimeOffset(1998, 4, 2, 10, 0, 0, TimeSpan.Zero)
                    }
                ],
                WkoFirmaId = Guid.Parse("11111111-1111-4111-8111-111111111104"),
                DetailUrl = "https://firmen.wko.at/salzkammergut-immobilientreuhand-gmbh/oberösterreich/?firmaid=11111111-1111-4111-8111-111111111104&standortid=3&standortname=oberösterreich%20(bundesland)&suchbegriff=immobilientreuhänder",
                SourceSearchTerm = "Immobilientreuhänder",
                IsActive = true,
                FirstSeenAt = now,
                LastScrapedAt = now
            },
            new()
            {
                Name = "Innviertler Immobilienbüro Rieder",
                CategoryText = "Immobilienbüro",
                Street = "Hauptplatz 8",
                PostalCode = "4910",
                City = "Ried im Innkreis",
                Phones = ["07752 33445"],
                Email = "buero@rieder-immobilien.at",
                Website = "https://www.rieder-immobilien.at",
                CompanyRegisterNumber = null,
                CompanyCourt = null,
                LegalForm = "e.U.",
                FoundedYear = 2019,
                FoundedDate = new DateTimeOffset(2019, 11, 18, 11, 0, 0, TimeSpan.Zero),
                IsTrainingCompany = true,
                Permits =
                [
                    new WkoCompanyPermit
                    {
                        FachgruppeName = "FG Immobilien- und Vermögenstreuhänder",
                        Description = "Immobilientreuhänder (Immobilienmakler, Immobilienverwalter, Bauträger), eingeschränkt auf Immobilienmakler",
                        ManagingDirector = "Sabine Wimmer",
                        GisaNumber = "20990011",
                        Since = new DateTimeOffset(2019, 11, 18, 11, 0, 0, TimeSpan.Zero)
                    }
                ],
                WkoFirmaId = Guid.Parse("11111111-1111-4111-8111-111111111105"),
                DetailUrl = "https://firmen.wko.at/innviertler-immobilienbuero-rieder/oberösterreich/?firmaid=11111111-1111-4111-8111-111111111105&standortid=3&standortname=oberösterreich%20(bundesland)&suchbegriff=immobilienbüro",
                SourceSearchTerm = "Immobilienbüro",
                IsActive = true,
                FirstSeenAt = now,
                LastScrapedAt = now
            },
            new()
            {
                Name = "Mühlviertler Grund & Boden Makler",
                CategoryText = "Immobilienmakler",
                Street = "Kirchenplatz 3",
                PostalCode = "4240",
                City = "Freistadt",
                Phones = ["07942 22110"],
                Email = "office@muehlviertel-immo.at",
                Website = null,
                CompanyRegisterNumber = null,
                CompanyCourt = null,
                LegalForm = "e.U.",
                FoundedYear = 2021,
                FoundedDate = new DateTimeOffset(2021, 6, 30, 10, 0, 0, TimeSpan.Zero),
                Permits =
                [
                    new WkoCompanyPermit
                    {
                        FachgruppeName = "FG Immobilien- und Vermögenstreuhänder",
                        Description = "Immobilientreuhänder (Immobilienmakler, Immobilienverwalter, Bauträger), eingeschränkt auf Immobilienmakler",
                        ManagingDirector = "Thomas Aigner",
                        GisaNumber = "21001122",
                        Since = new DateTimeOffset(2021, 6, 30, 10, 0, 0, TimeSpan.Zero)
                    }
                ],
                WkoFirmaId = Guid.Parse("11111111-1111-4111-8111-111111111106"),
                DetailUrl = "https://firmen.wko.at/muehlviertler-grund-boden-makler/oberösterreich/?firmaid=11111111-1111-4111-8111-111111111106&standortid=3&standortname=oberösterreich%20(bundesland)&suchbegriff=immobilienmakler",
                SourceSearchTerm = "Immobilienmakler",
                IsActive = true,
                FirstSeenAt = now,
                LastScrapedAt = now
            },
            new()
            {
                Name = "Vöcklabrucker Anlage Immobilien GmbH",
                CategoryText = "Immobilienmaklerin, Immobilienverwalterin",
                Street = "Salzburger Straße 17",
                PostalCode = "4840",
                City = "Vöcklabruck",
                Phones = ["07672 445566"],
                Email = "office@voecklabrucker-anlage.at",
                Website = "https://www.voecklabrucker-anlage.at",
                CompanyRegisterNumber = "455123v",
                CompanyCourt = "Landesgericht Wels",
                LegalForm = "GmbH",
                FoundedYear = 2013,
                FoundedDate = new DateTimeOffset(2013, 2, 14, 11, 0, 0, TimeSpan.Zero),
                Permits =
                [
                    new WkoCompanyPermit
                    {
                        FachgruppeName = "FG Immobilien- und Vermögenstreuhänder",
                        Description = "Immobilientreuhänderin (Immobilienmaklerin, Immobilienverwalterin, Bauträgerin)",
                        ManagingDirector = "Julia Reisinger",
                        GisaNumber = "21223344",
                        Since = new DateTimeOffset(2013, 2, 14, 11, 0, 0, TimeSpan.Zero)
                    }
                ],
                WkoFirmaId = Guid.Parse("11111111-1111-4111-8111-111111111107"),
                DetailUrl = "https://firmen.wko.at/voecklabrucker-anlage-immobilien-gmbh/oberösterreich/?firmaid=11111111-1111-4111-8111-111111111107&standortid=3&standortname=oberösterreich%20(bundesland)&suchbegriff=immobilienmaklerin",
                SourceSearchTerm = "Immobilienmaklerin",
                IsActive = true,
                FirstSeenAt = now,
                LastScrapedAt = now
            },
            new()
            {
                Name = "Innkreis Wohntraum Vermittlung",
                CategoryText = "Immobilienmakler, Immobilienverwalter",
                Street = "Stadtplatz 5",
                PostalCode = "5280",
                City = "Braunau am Inn",
                Phones = ["07722 887766"],
                Email = "info@innkreis-wohntraum.at",
                Website = "https://www.innkreis-wohntraum.at",
                CompanyRegisterNumber = "533221w",
                CompanyCourt = "Landesgericht Ried im Innkreis",
                LegalForm = "OG",
                FoundedYear = 2008,
                FoundedDate = new DateTimeOffset(2008, 8, 25, 10, 0, 0, TimeSpan.Zero),
                Permits =
                [
                    new WkoCompanyPermit
                    {
                        FachgruppeName = "FG Immobilien- und Vermögenstreuhänder",
                        Description = "Immobilientreuhänder (Immobilienmakler, Immobilienverwalter, Bauträger)",
                        ManagingDirector = "Werner Huber",
                        GisaNumber = "21445566",
                        Since = new DateTimeOffset(2008, 8, 25, 10, 0, 0, TimeSpan.Zero)
                    }
                ],
                WkoFirmaId = Guid.Parse("11111111-1111-4111-8111-111111111108"),
                DetailUrl = "https://firmen.wko.at/innkreis-wohntraum-vermittlung/oberösterreich/?firmaid=11111111-1111-4111-8111-111111111108&standortid=3&standortname=oberösterreich%20(bundesland)&suchbegriff=immobilienmakler",
                SourceSearchTerm = "Immobilienmakler",
                IsActive = true,
                FirstSeenAt = now,
                LastScrapedAt = now
            }
        };

        dbContext.Set<WkoCompany>().AddRange(companies);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
