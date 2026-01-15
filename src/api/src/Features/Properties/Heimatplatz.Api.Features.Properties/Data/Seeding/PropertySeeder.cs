using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Core.Data.Seeding;
using Heimatplatz.Api.Features.Properties.Contracts;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Heimatplatz.Api.Features.Properties.Data.Seeding;

/// <summary>
/// Seeder fuer Beispiel-Immobilien in Oberoesterreich
/// </summary>
public class PropertySeeder(AppDbContext dbContext) : ISeeder
{
    public int Order => 10;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.Set<Property>().AnyAsync(cancellationToken))
            return;

        var properties = new List<Property>
        {
            // Haeuser
            new()
            {
                Id = Guid.NewGuid(),
                Titel = "Einfamilienhaus in Linz-Urfahr",
                Adresse = "Hauptstrasse 15",
                Ort = "Linz",
                Plz = "4040",
                Preis = 349000,
                WohnflaecheM2 = 145,
                GrundstuecksflaecheM2 = 520,
                Zimmer = 5,
                Baujahr = 2018,
                Typ = PropertyType.Haus,
                AnbieterTyp = SellerType.Makler,
                AnbieterName = "Mustermann Immobilien",
                Beschreibung = "Wunderschoenes Einfamilienhaus mit grossem Garten in ruhiger Lage. Hochwertige Ausstattung, Fussbodenheizung, Photovoltaikanlage.",
                Ausstattung = ["Garage", "Garten", "Terrasse", "Keller", "Fussbodenheizung", "Photovoltaik"],
                BildUrls = ["https://picsum.photos/seed/haus1/800/600"]
            },
            new()
            {
                Id = Guid.NewGuid(),
                Titel = "Modernes Reihenhaus in Wels",
                Adresse = "Ringstrasse 42",
                Ort = "Wels",
                Plz = "4600",
                Preis = 289000,
                WohnflaecheM2 = 120,
                GrundstuecksflaecheM2 = 180,
                Zimmer = 4,
                Baujahr = 2020,
                Typ = PropertyType.Haus,
                AnbieterTyp = SellerType.Privat,
                AnbieterName = "Familie Huber",
                Beschreibung = "Neuwertiges Reihenhaus in zentraler Lage. Perfekt fuer junge Familien. Kurze Wege zu Schulen und Geschaeften.",
                Ausstattung = ["Carport", "Terrasse", "Keller", "Fussbodenheizung"],
                BildUrls = ["https://picsum.photos/seed/haus2/800/600"]
            },
            new()
            {
                Id = Guid.NewGuid(),
                Titel = "Villa am Traunsee",
                Adresse = "Seeuferweg 8",
                Ort = "Gmunden",
                Plz = "4810",
                Preis = 890000,
                WohnflaecheM2 = 220,
                GrundstuecksflaecheM2 = 1200,
                Zimmer = 7,
                Baujahr = 2015,
                Typ = PropertyType.Haus,
                AnbieterTyp = SellerType.Makler,
                AnbieterName = "Luxus Immobilien GmbH",
                Beschreibung = "Exklusive Villa mit direktem Seezugang. Panoramablick auf den Traunsee. Hochwertigste Ausstattung.",
                Ausstattung = ["Doppelgarage", "Pool", "Sauna", "Seezugang", "Smarthome", "Klimaanlage"],
                BildUrls = ["https://picsum.photos/seed/villa1/800/600"]
            },
            new()
            {
                Id = Guid.NewGuid(),
                Titel = "Landhaus in Bad Ischl",
                Adresse = "Kaiserweg 23",
                Ort = "Bad Ischl",
                Plz = "4820",
                Preis = 425000,
                WohnflaecheM2 = 165,
                GrundstuecksflaecheM2 = 850,
                Zimmer = 5,
                Baujahr = 1998,
                Typ = PropertyType.Haus,
                AnbieterTyp = SellerType.Privat,
                AnbieterName = "Herr Maier",
                Beschreibung = "Charmantes Landhaus im Salzkammergut. Renoviert mit Liebe zum Detail. Idealer Rueckzugsort.",
                Ausstattung = ["Garage", "Garten", "Kachelofen", "Keller", "Dachboden"],
                BildUrls = ["https://picsum.photos/seed/landhaus1/800/600"]
            },
            new()
            {
                Id = Guid.NewGuid(),
                Titel = "Familienhaus in Steyr",
                Adresse = "Bahnhofstrasse 67",
                Ort = "Steyr",
                Plz = "4400",
                Preis = 315000,
                WohnflaecheM2 = 135,
                GrundstuecksflaecheM2 = 450,
                Zimmer = 5,
                Baujahr = 2010,
                Typ = PropertyType.Haus,
                AnbieterTyp = SellerType.Makler,
                AnbieterName = "Immobilien Steyr",
                Beschreibung = "Gepflegtes Einfamilienhaus in guter Lage. Nahe Stadtzentrum und Naturgebiet.",
                Ausstattung = ["Garage", "Garten", "Terrasse", "Keller"],
                BildUrls = ["https://picsum.photos/seed/haus3/800/600"]
            },

            // Grundstuecke
            new()
            {
                Id = Guid.NewGuid(),
                Titel = "Baugrundstück in Wels",
                Adresse = "Neubaugebiet Sued",
                Ort = "Wels",
                Plz = "4600",
                Preis = 189000,
                GrundstuecksflaecheM2 = 850,
                Typ = PropertyType.Grundstueck,
                AnbieterTyp = SellerType.Privat,
                AnbieterName = "Familie Mueller",
                Beschreibung = "Voll erschlossenes Baugrundstuck in ruhiger Wohnlage. Alle Anschluesse vorhanden.",
                Ausstattung = ["Erschlossen", "Strom", "Wasser", "Kanal", "Gas"],
                BildUrls = ["https://picsum.photos/seed/grund1/800/600"]
            },
            new()
            {
                Id = Guid.NewGuid(),
                Titel = "Sonniges Baugrundstück Linz-Land",
                Adresse = "Am Sonnenhang 12",
                Ort = "Leonding",
                Plz = "4060",
                Preis = 245000,
                GrundstuecksflaecheM2 = 720,
                Typ = PropertyType.Grundstueck,
                AnbieterTyp = SellerType.Makler,
                AnbieterName = "Grund & Boden OOe",
                Beschreibung = "Suedhanglage mit herrlichem Ausblick. Bebauungsplan liegt vor.",
                Ausstattung = ["Erschlossen", "Suedlage", "Aussicht"],
                BildUrls = ["https://picsum.photos/seed/grund2/800/600"]
            },
            new()
            {
                Id = Guid.NewGuid(),
                Titel = "Grosses Baugrundstück Muehlviertel",
                Adresse = "Dorfstrasse",
                Ort = "Freistadt",
                Plz = "4240",
                Preis = 95000,
                GrundstuecksflaecheM2 = 1200,
                Typ = PropertyType.Grundstueck,
                AnbieterTyp = SellerType.Privat,
                AnbieterName = "Gemeinde Freistadt",
                Beschreibung = "Guenstiges Baugrundstuck im schoenen Muehlviertel. Ruhige Lage, gute Infrastruktur.",
                Ausstattung = ["Teilerschlossen", "Strom", "Wasser"],
                BildUrls = ["https://picsum.photos/seed/grund3/800/600"]
            },

            // Zwangsversteigerungen
            new()
            {
                Id = Guid.NewGuid(),
                Titel = "Zwangsversteigerung: Haus in Traun",
                Adresse = "Industriestrasse 45",
                Ort = "Traun",
                Plz = "4050",
                Preis = 185000,
                WohnflaecheM2 = 110,
                GrundstuecksflaecheM2 = 380,
                Zimmer = 4,
                Baujahr = 1985,
                Typ = PropertyType.Zwangsversteigerung,
                AnbieterTyp = SellerType.Makler,
                AnbieterName = "Bezirksgericht Linz",
                Beschreibung = "Aelteres Haus mit Renovierungsbedarf. Versteigerungstermin: naechsten Monat. Besichtigung moeglich.",
                Ausstattung = ["Garage", "Keller"],
                BildUrls = ["https://picsum.photos/seed/zwang1/800/600"]
            },
            new()
            {
                Id = Guid.NewGuid(),
                Titel = "Zwangsversteigerung: Grundstück Enns",
                Adresse = "Feldweg 3",
                Ort = "Enns",
                Plz = "4470",
                Preis = 68000,
                GrundstuecksflaecheM2 = 650,
                Typ = PropertyType.Zwangsversteigerung,
                AnbieterTyp = SellerType.Makler,
                AnbieterName = "Bezirksgericht Steyr",
                Beschreibung = "Baugrundstuck aus Zwangsversteigerung. Gute Lage, erschlossen.",
                Ausstattung = ["Erschlossen"],
                BildUrls = ["https://picsum.photos/seed/zwang2/800/600"]
            },

            // Weitere Haeuser
            new()
            {
                Id = Guid.NewGuid(),
                Titel = "Bungalow in Braunau",
                Adresse = "Gartenstrasse 18",
                Ort = "Braunau am Inn",
                Plz = "5280",
                Preis = 275000,
                WohnflaecheM2 = 95,
                GrundstuecksflaecheM2 = 600,
                Zimmer = 3,
                Baujahr = 2005,
                Typ = PropertyType.Haus,
                AnbieterTyp = SellerType.Privat,
                AnbieterName = "Ehepaar Schmidt",
                Beschreibung = "Barrierefreier Bungalow, ideal fuer Senioren. Pflegeleichter Garten.",
                Ausstattung = ["Carport", "Garten", "Barrierefrei", "Fussbodenheizung"],
                BildUrls = ["https://picsum.photos/seed/bungalow1/800/600"]
            },
            new()
            {
                Id = Guid.NewGuid(),
                Titel = "Doppelhaushälfte Vöcklabruck",
                Adresse = "Schulweg 7",
                Ort = "Voecklabruck",
                Plz = "4840",
                Preis = 298000,
                WohnflaecheM2 = 125,
                GrundstuecksflaecheM2 = 280,
                Zimmer = 4,
                Baujahr = 2019,
                Typ = PropertyType.Haus,
                AnbieterTyp = SellerType.Makler,
                AnbieterName = "Hausfreund Immobilien",
                Beschreibung = "Neuwertige Doppelhaushaelfte in familienfreundlicher Lage. Schulen und Kindergarten in Gehweite.",
                Ausstattung = ["Garage", "Garten", "Terrasse", "Fussbodenheizung", "Waermepumpe"],
                BildUrls = ["https://picsum.photos/seed/doppel1/800/600"]
            }
        };

        dbContext.Set<Property>().AddRange(properties);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
