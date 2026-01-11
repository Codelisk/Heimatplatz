using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Core.Data.Seeding;
using Heimatplatz.Api.Features.Immobilien.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Immobilien.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Heimatplatz.Api.Features.Immobilien.Seeding;

/// <summary>
/// Seeder fuer realistische Immobilien-Testdaten aus Oberoesterreich
/// Basierend auf aktuellen Marktpreisen (Stand 2025):
/// - Linz: ~5.160 EUR/m² (Haeuser)
/// - Gmunden: ~4.340 EUR/m²
/// - Voecklabruck: ~5.172 EUR/m²
/// - Freistadt: ~1.946 EUR/m²
/// - Grundstuecke Attersee: 1.700-3.200 EUR/m²
/// </summary>
public class ImmobilienSeeder(AppDbContext dbContext, ILogger<ImmobilienSeeder> logger) : ISeeder
{
    public int Order => 10;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.Set<Immobilie>().AnyAsync(cancellationToken))
        {
            logger.LogInformation("Immobilien bereits vorhanden, Seeding uebersprungen");
            return;
        }

        logger.LogInformation("Seede Immobilien aus Oberoesterreich...");

        var immobilien = CreateImmobilien();
        dbContext.Set<Immobilie>().AddRange(immobilien);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("{Count} Immobilien erfolgreich geseeded", immobilien.Count);
    }

    private static List<Immobilie> CreateImmobilien() =>
    [
        // Premium: Linz Poestlingberg
        new Immobilie
        {
            Titel = "VILLA POESTLINGBERG",
            Beschreibung = "Exklusive Hangvilla mit atemberaubendem Panoramablick ueber Linz. Hochwertige Ausstattung, grosszuegiger Garten mit Pool, Doppelgarage. Ruhige Lage nahe der Poestlingbergbahn.",
            Typ = ImmobilienTyp.Haus,
            Preis = 1_850_000m,
            Wohnflaeche = 280m,
            Grundstuecksflaeche = 1200m,
            Ort = "Linz",
            Bezirk = "Poestlingberg",
            Region = "Oberoesterreich",
            Breitengrad = 48.3234,
            Laengengrad = 14.2589,
            Zimmer = 7,
            Schlafzimmer = 4,
            Badezimmer = 3,
            Baujahr = 2018,
            ZusatzInfo = "7 Zimmer",
            Bilder =
            [
                new ImmobilieBild { Url = "https://picsum.photos/seed/linz1/800/600", AltText = "Villa Poestlingberg Aussenansicht", IstHauptbild = true, Reihenfolge = 0 },
                new ImmobilieBild { Url = "https://picsum.photos/seed/linz2/800/600", AltText = "Wohnzimmer mit Panoramafenster", Reihenfolge = 1 }
            ]
        },

        // Seegrundstück Attersee
        new Immobilie
        {
            Titel = "SEEGRUNDSTÜCK SEEWALCHEN",
            Beschreibung = "Einzigartiges Baugrundstück mit direktem Seezugang am Attersee. Suedwestausrichtung, unverbaubarer Seeblick. Widmung: Wohngebiet. Anschlüsse vorhanden.",
            Typ = ImmobilienTyp.Grundstueck,
            Preis = 1_450_000m,
            Wohnflaeche = 0m,
            Grundstuecksflaeche = 520m,
            Ort = "Seewalchen",
            Bezirk = "Attersee",
            Region = "Oberoesterreich",
            Breitengrad = 47.9456,
            Laengengrad = 13.5892,
            ZusatzInfo = "Seezugang",
            Bilder =
            [
                new ImmobilieBild { Url = "https://picsum.photos/seed/attersee1/800/600", AltText = "Seegrundstück Seewalchen", IstHauptbild = true, Reihenfolge = 0 }
            ]
        },

        // Gmunden Traunsee
        new Immobilie
        {
            Titel = "SEEBLICK GMUNDEN",
            Beschreibung = "Stilvolle Villa in bester Hanglage mit freiem Blick auf den Traunsee und das Tote Gebirge. Grosszuegige Raeume, moderner Ausbaustandard, Wellnessbereich.",
            Typ = ImmobilienTyp.Haus,
            Preis = 2_100_000m,
            Wohnflaeche = 320m,
            Grundstuecksflaeche = 1500m,
            Ort = "Gmunden",
            Bezirk = "Traunsee",
            Region = "Oberoesterreich",
            Breitengrad = 47.9186,
            Laengengrad = 13.7987,
            Zimmer = 8,
            Schlafzimmer = 5,
            Badezimmer = 3,
            Baujahr = 2015,
            ZusatzInfo = "8 Zimmer",
            Bilder =
            [
                new ImmobilieBild { Url = "https://picsum.photos/seed/gmunden1/800/600", AltText = "Villa Gmunden Seeblick", IstHauptbild = true, Reihenfolge = 0 },
                new ImmobilieBild { Url = "https://picsum.photos/seed/gmunden2/800/600", AltText = "Terrasse mit Bergpanorama", Reihenfolge = 1 }
            ]
        },

        // Wels Zentrum
        new Immobilie
        {
            Titel = "STADTHAUS WELS",
            Beschreibung = "Modernes Stadthaus im Herzen von Wels. Perfekte Infrastruktur, kurze Wege zu Schulen und Einkaufsmoeglichkeiten. Garage und kleiner Garten.",
            Typ = ImmobilienTyp.Haus,
            Preis = 485_000m,
            Wohnflaeche = 145m,
            Grundstuecksflaeche = 280m,
            Ort = "Wels",
            Bezirk = "Puchberg",
            Region = "Oberoesterreich",
            Breitengrad = 48.1594,
            Laengengrad = 14.0289,
            Zimmer = 5,
            Schlafzimmer = 3,
            Badezimmer = 2,
            Baujahr = 2020,
            ZusatzInfo = "5 Zimmer",
            Bilder =
            [
                new ImmobilieBild { Url = "https://picsum.photos/seed/wels1/800/600", AltText = "Stadthaus Wels", IstHauptbild = true, Reihenfolge = 0 }
            ]
        },

        // Mühlviertel günstig
        new Immobilie
        {
            Titel = "LANDHAUS MUEHLVIERTEL",
            Beschreibung = "Charmantes Landhaus im gruenen Muehlviertel. Ideal fuer Naturliebhaber und Familien. Grosser Garten, Nebengebaeude, ruhige Lage.",
            Typ = ImmobilienTyp.Haus,
            Preis = 320_000m,
            Wohnflaeche = 180m,
            Grundstuecksflaeche = 2500m,
            Ort = "Freistadt",
            Bezirk = "Muehlviertel",
            Region = "Oberoesterreich",
            Breitengrad = 48.5108,
            Laengengrad = 14.5042,
            Zimmer = 6,
            Schlafzimmer = 4,
            Badezimmer = 2,
            Baujahr = 1985,
            ZusatzInfo = "6 Zimmer",
            Bilder =
            [
                new ImmobilieBild { Url = "https://picsum.photos/seed/freistadt1/800/600", AltText = "Landhaus Muehlviertel", IstHauptbild = true, Reihenfolge = 0 }
            ]
        },

        // Leonding modern
        new Immobilie
        {
            Titel = "MODERNE STADTVILLA LEONDING",
            Beschreibung = "Neubau 2025: Hochwertige Doppelhaushälfte in beliebter Wohnlage. Smart-Home-Ausstattung, Luftwaermepumpe, Photovoltaik. Erstbezug!",
            Typ = ImmobilienTyp.Haus,
            Preis = 750_000m,
            Wohnflaeche = 165m,
            Grundstuecksflaeche = 350m,
            Ort = "Leonding",
            Bezirk = "Linz-Land",
            Region = "Oberoesterreich",
            Breitengrad = 48.2789,
            Laengengrad = 14.2456,
            Zimmer = 5,
            Schlafzimmer = 3,
            Badezimmer = 2,
            Baujahr = 2025,
            ZusatzInfo = "Erstbezug",
            Bilder =
            [
                new ImmobilieBild { Url = "https://picsum.photos/seed/leonding1/800/600", AltText = "Neubau Leonding", IstHauptbild = true, Reihenfolge = 0 },
                new ImmobilieBild { Url = "https://picsum.photos/seed/leonding2/800/600", AltText = "Moderne Kueche", Reihenfolge = 1 }
            ]
        },

        // Baugrund Vöcklabruck
        new Immobilie
        {
            Titel = "BAUGRUND VOECKLABRUCK",
            Beschreibung = "Ebenes, sonniges Baugrundstück in ruhiger Siedlungslage. Alle Anschluesse an der Grundstuecksgrenze. Bebauungsplan liegt vor.",
            Typ = ImmobilienTyp.Grundstueck,
            Preis = 125_000m,
            Wohnflaeche = 0m,
            Grundstuecksflaeche = 720m,
            Ort = "Voecklabruck",
            Region = "Oberoesterreich",
            Breitengrad = 48.0067,
            Laengengrad = 13.6578,
            ZusatzInfo = "720 m²",
            Bilder =
            [
                new ImmobilieBild { Url = "https://picsum.photos/seed/vb1/800/600", AltText = "Baugrund Voecklabruck", IstHauptbild = true, Reihenfolge = 0 }
            ]
        },

        // Steyr Altstadt
        new Immobilie
        {
            Titel = "ALTBAU STEYR ZENTRUM",
            Beschreibung = "Liebevoll saniertes Buergerhaus in der historischen Altstadt von Steyr. Originalsubstanz erhalten, moderne Haustechnik. Garagen moeglich.",
            Typ = ImmobilienTyp.Haus,
            Preis = 380_000m,
            Wohnflaeche = 160m,
            Ort = "Steyr",
            Bezirk = "Altstadt",
            Region = "Oberoesterreich",
            Breitengrad = 48.0389,
            Laengengrad = 14.4214,
            Zimmer = 6,
            Schlafzimmer = 3,
            Badezimmer = 2,
            Baujahr = 1890,
            ZusatzInfo = "Denkmalschutz",
            Bilder =
            [
                new ImmobilieBild { Url = "https://picsum.photos/seed/steyr1/800/600", AltText = "Altbau Steyr", IstHauptbild = true, Reihenfolge = 0 }
            ]
        },

        // Traunkirchen Seelage
        new Immobilie
        {
            Titel = "SEEGRUNDSTÜCK TRAUNKIRCHEN",
            Beschreibung = "Seltene Gelegenheit: Baugrund direkt am Traunsee mit eigenem Seezugang und Bootsanleger. Traumhafte Aussicht auf die Bergkulisse.",
            Typ = ImmobilienTyp.Grundstueck,
            Preis = 1_680_000m,
            Wohnflaeche = 0m,
            Grundstuecksflaeche = 680m,
            Ort = "Traunkirchen",
            Bezirk = "Gmunden",
            Region = "Oberoesterreich",
            Breitengrad = 47.8523,
            Laengengrad = 13.7856,
            ZusatzInfo = "Seezugang",
            Bilder =
            [
                new ImmobilieBild { Url = "https://picsum.photos/seed/traunkirchen1/800/600", AltText = "Seegrundstück Traunkirchen", IstHauptbild = true, Reihenfolge = 0 }
            ]
        },

        // Bad Ischl Neubau
        new Immobilie
        {
            Titel = "NEUBAU BAD ISCHL",
            Beschreibung = "Modernes Einfamilienhaus in der Kulturhauptstadt 2024. Energieeffiziente Bauweise (A++), sonnige Suedlage, gepflegter Garten.",
            Typ = ImmobilienTyp.Haus,
            Preis = 620_000m,
            Wohnflaeche = 140m,
            Grundstuecksflaeche = 600m,
            Ort = "Bad Ischl",
            Bezirk = "Gmunden",
            Region = "Oberoesterreich",
            Breitengrad = 47.7116,
            Laengengrad = 13.6195,
            Zimmer = 5,
            Schlafzimmer = 3,
            Badezimmer = 2,
            Baujahr = 2023,
            ZusatzInfo = "5 Zimmer",
            Bilder =
            [
                new ImmobilieBild { Url = "https://picsum.photos/seed/badischl1/800/600", AltText = "Neubau Bad Ischl", IstHauptbild = true, Reihenfolge = 0 }
            ]
        },

        // Linz Urfahr Wohnung
        new Immobilie
        {
            Titel = "PENTHOUSE URFAHR",
            Beschreibung = "Exklusive Penthouse-Wohnung mit Dachterrasse in Linz-Urfahr. Hochwertige Ausstattung, 2 Tiefgaragenplaetze, Concierge-Service.",
            Typ = ImmobilienTyp.Wohnung,
            Preis = 890_000m,
            Wohnflaeche = 145m,
            Ort = "Linz",
            Bezirk = "Urfahr",
            Region = "Oberoesterreich",
            Breitengrad = 48.3123,
            Laengengrad = 14.2867,
            Zimmer = 4,
            Schlafzimmer = 2,
            Badezimmer = 2,
            Baujahr = 2022,
            ZusatzInfo = "Dachterrasse",
            Bilder =
            [
                new ImmobilieBild { Url = "https://picsum.photos/seed/urfahr1/800/600", AltText = "Penthouse Urfahr", IstHauptbild = true, Reihenfolge = 0 },
                new ImmobilieBild { Url = "https://picsum.photos/seed/urfahr2/800/600", AltText = "Dachterrasse mit Blick", Reihenfolge = 1 }
            ]
        },

        // Mondsee
        new Immobilie
        {
            Titel = "LANDHAUS MONDSEE",
            Beschreibung = "Traditionelles Landhaus im Salzkammergut nahe Mondsee. Grosser Obstgarten, Werkstatt, ideal fuer Pferdehaltung geeignet.",
            Typ = ImmobilienTyp.Haus,
            Preis = 980_000m,
            Wohnflaeche = 220m,
            Grundstuecksflaeche = 4500m,
            Ort = "Mondsee",
            Bezirk = "Voecklabruck",
            Region = "Oberoesterreich",
            Breitengrad = 47.8561,
            Laengengrad = 13.3489,
            Zimmer = 7,
            Schlafzimmer = 4,
            Badezimmer = 2,
            Baujahr = 1965,
            ZusatzInfo = "Pferdehaltung",
            Bilder =
            [
                new ImmobilieBild { Url = "https://picsum.photos/seed/mondsee1/800/600", AltText = "Landhaus Mondsee", IstHauptbild = true, Reihenfolge = 0 }
            ]
        }
    ];
}
