namespace Heimatplatz.Api.Features.Properties.Data.Seeding;

/// <summary>
/// Kanonische Beschreibungstexte der Seed-Immobilien (Titel -> Beschreibung).
/// Einzige Quelle für PropertySeeder (leere DB) und
/// PropertyDescriptionRefreshSeeder (Bestands-DBs).
///
/// Die Texte sind bewusst realistisch lang gehalten (Absätze + "*"-Aufzählungen
/// wie beim OpenImmo-Import), damit der Web-Leporello-Falz und das
/// Listen-Rendering mit Testdaten sichtbar sind. Drei Objekte (die beiden
/// Zwangsversteigerungen und der Bungalow) bleiben absichtlich unter der
/// Falz-Schwelle von 900 Zeichen - so bleibt auch der Ungefaltet-Pfad testbar.
/// </summary>
internal static class PropertySeedDescriptions
{
    internal static readonly Dictionary<string, string> ByTitle = new()
    {
        ["Einfamilienhaus in Linz-Urfahr"] =
            """
            Wunderschönes Einfamilienhaus mit großem Garten in ruhiger Lage von Linz-Urfahr. Das 2018 errichtete Haus überzeugt durch seine hochwertige Ausstattung und den durchdachten Grundriss – hier ziehen Sie ein und fühlen sich vom ersten Tag an zu Hause.

            Im Erdgeschoss erwartet Sie ein offener Wohn- und Essbereich mit direktem Zugang zur überdachten Terrasse, dazu eine moderne Einbauküche mit Markengeräten, ein Gäste-WC und ein praktischer Abstellraum. Das Obergeschoss bietet drei helle Schlafzimmer, ein geräumiges Badezimmer mit Wanne und Dusche sowie einen Balkon mit Blick ins Grüne.

            * Fußbodenheizung in allen Räumen (Luft-Wärmepumpe)
            * Photovoltaikanlage mit 9,8 kWp
            * Dreifach verglaste Fenster mit Außenjalousien
            * Doppelgarage mit Vorbereitung für E-Ladestation
            * Vollkeller mit Werkstatt- und Lagerräumen

            Der rund 520 m² große Garten ist eingewachsen und pflegeleicht, die Hecke schützt vor neugierigen Blicken. Kindergarten, Volksschule und Nahversorger erreichen Sie in wenigen Gehminuten, die Straßenbahn bringt Sie in einer Viertelstunde ins Linzer Zentrum.

            HWB: 38 kWh/m²a, Klasse B. Übergabe nach Vereinbarung. Wir freuen uns auf Ihre Terminanfrage zur Besichtigung.
            """,

        ["Modernes Reihenhaus in Wels"] =
            """
            Schlüsselfertiges Neubau-Reihenhaus in zentraler Welser Lage – Fertigstellung Ende 2027. Im Projekt „Wohnen am Ring" entstehen acht Reihenhäuser in massiver Ziegelbauweise, geplant von einem regionalen Baumeister mit Fixpreisgarantie.

            Auf 120 m² Wohnfläche verteilen sich vier Zimmer über zwei Geschosse: unten der offene Wohn-, Ess- und Küchenbereich mit Terrassenzugang, oben drei Schlafzimmer und das Familienbad. Der nach Westen ausgerichtete Eigengarten mit rund 60 m², ein Carport und ein Geräteraum sind im Kaufpreis enthalten.

            * Luft-Wärmepumpe mit Fußbodenheizung, vorbereitet für Kühlfunktion
            * Photovoltaikanlage inklusive
            * Bodenbeläge, Innentüren und Sanitärausstattung frei bemusterbar
            * Belagsfertige Übergabe zum Fixpreis, keine Nachverrechnung

            Schulen, Kindergarten und Geschäfte des täglichen Bedarfs liegen in Gehweite, den Bahnhof Wels erreichen Sie in zehn Minuten mit dem Rad. Ideal für junge Familien, die ihr Zuhause von Anfang an mitgestalten möchten.

            HWB: 25 kWh/m²a (Planungswert laut Energieausweis). Provisionsfrei direkt vom Bauträger.
            """,

        ["Villa am Traunsee"] =
            """
            Exklusive Villa mit direktem Seezugang und Panoramablick über den Traunsee bis zum Traunstein. Anwesen wie dieses kommen nur selten auf den Markt: ein gewachsenes Grundstück mit 1.200 m², ein eigenes Bootshaus und ein Badeplatz, der ausschließlich Ihnen gehört.

            Die Villa wurde 2015 umfassend erneuert und verbindet klassische Architektur mit zeitgemäßem Komfort. Auf der Wohnebene empfängt Sie ein großzügiger Salon mit offenem Kamin und raumhoher Verglasung zum See, ergänzt um eine Küche mit angeschlossener Speis und einen seeseitigen Essplatz. Im Obergeschoss liegen vier Schlafzimmer, zwei Bäder und ein Ankleidezimmer; das Dachgeschoss ist als Studio mit eigener Seeterrasse ausgebaut.

            * Direkter Seezugang mit Badeplatz und Steg
            * Bootshaus mit Liegeplatz
            * Sauna mit Panoramafenster
            * Doppelgarage plus zwei Außenstellplätze
            * Smart-Home-Steuerung für Heizung, Beschattung und Alarmanlage

            Gmunden mit seiner Esplanade, den Schulen und der Anbindung an die Salzkammergutbahn liegt nur wenige Fahrminuten entfernt. Der parkähnliche Garten mit altem Baumbestand bietet ganztägig Sonne und völlige Privatsphäre.

            Aus Diskretionsgründen übermitteln wir Ihnen das ausführliche Exposé samt Adresse nach einem persönlichen Erstgespräch. HWB: 62 kWh/m²a.
            """,

        ["Landhaus in Bad Ischl"] =
            """
            Charmantes Landhaus im Herzen des Salzkammerguts, mit viel Liebe zum Detail renoviert und sofort beziehbar. Wer den Charakter alter Häuser sucht, ohne auf zeitgemäßen Komfort zu verzichten, wird hier fündig.

            Hinter der traditionellen Holzfassade verbirgt sich ein behutsam modernisiertes Zuhause: Die Stube mit original Kachelofen bildet das Herzstück des Erdgeschosses, daneben liegen Küche, Speis und ein Bad mit bodengleicher Dusche. Im Obergeschoss befinden sich drei Zimmer mit Holzböden und der Blick auf die umliegenden Berge.

            * Elektro- und Sanitärinstallationen komplett erneuert (2019)
            * Neue Holzfenster mit Dreifachverglasung
            * Kachelofen plus Zentralheizung
            * Nebengebäude mit Werkstatt und Holzlage
            * 850 m² Garten mit altem Obstbaumbestand

            Die Kaiserstadt Bad Ischl bietet Ihnen alles für den täglichen Bedarf, dazu Theater, Therme und ein Netz an Wander- und Langlaufwegen direkt vor der Haustür. Die Katrin-Seilbahn und der Wolfgangsee sind in wenigen Minuten erreichbar.

            HWB: 96 kWh/m²a. Ein idealer Hauptwohnsitz für Liebhaber – auf Wunsch mit einem Teil des Mobiliars.
            """,

        ["Familienhaus in Steyr"] =
            """
            Gepflegtes Einfamilienhaus in guter Lage von Steyr – nahe dem Stadtzentrum und doch nur wenige Schritte vom Naherholungsgebiet an der Enns entfernt. Das 2010 errichtete Haus wurde laufend instand gehalten und ist sofort bezugsfertig.

            Der Grundriss ist auf Familien zugeschnitten: Wohnzimmer mit Essbereich und Terrassenzugang, separate Küche, im Obergeschoss drei Schlafzimmer und ein Bad mit Wanne und Dusche. Der Keller bietet Waschküche, Technik- und Hobbyraum.

            * Ziegelmassivbauweise mit Vollwärmeschutz
            * Gas-Brennwertheizung, Warmwasser über Solaranlage
            * Terrasse mit elektrischer Markise
            * Garage plus Stellplatz vor dem Haus

            Volksschule, Neue Mittelschule und der Stadtplatz sind zu Fuß erreichbar; über die Umfahrung sind Sie rasch in Linz oder im Ennstal. Der 450 m² große Garten ist nach Süden ausgerichtet und komplett eingezäunt.

            HWB: 54 kWh/m²a. Besichtigungen sind ab sofort möglich – gerne auch am Wochenende.
            """,

        ["Baugrundstück in Wels"] =
            """
            Voll erschlossenes Baugrundstück in ruhiger Wohnlage im Süden von Wels. Das ebene, annähernd rechteckige Grundstück mit 850 m² liegt in einer gewachsenen Siedlung mit gepflegten Einfamilienhäusern – ohne Durchzugsverkehr, aber mit rascher Anbindung an die Stadt.

            * Widmung: Bauland-Wohngebiet
            * Alle Anschlüsse an der Grundgrenze: Strom, Wasser, Kanal, Gas, Glasfaser
            * Keine Bauträgerbindung, keine Architektenbindung
            * Bebauungsplan mit offener Bauweise, zwei Geschosse zulässig

            Der Baugrund ist sofort bebaubar, ein aktuelles Baugrundgutachten liegt vor und wird Kaufinteressenten gerne zur Verfügung gestellt. Die Nachbargrundstücke sind bereits bebaut, Sie kaufen also ohne Überraschungen in einer fertigen Siedlung.

            Kindergarten und Volksschule liegen im Ort, das Einkaufszentrum an der B1 ist in fünf Autominuten erreichbar. Ein Grundstück für alle, die kurzfristig ihr Traumhaus verwirklichen möchten.
            """,

        ["Sonniges Baugrundstück Linz-Land"] =
            """
            Südhanglage mit herrlichem Ausblick über das Linzer Becken – dieses Baugrundstück in Leonding verbindet Aussichtslage mit Stadtnähe. Auf 720 m² planen Sie Ihr Haus dort, wo andere gerne spazieren gehen.

            Der rechtsgültige Bebauungsplan lässt eine offene oder gekuppelte Bauweise mit bis zu zwei Geschossen zu; durch die Hanglage bietet sich ein zusätzliches Untergeschoss mit direktem Gartenausgang an.

            * Widmung: Bauland-Wohngebiet
            * Strom, Wasser und Kanal an der Grundgrenze
            * Südausrichtung mit unverbaubarem Weitblick
            * Keine Bauverpflichtung

            Die Lage vereint das Beste aus zwei Welten: In wenigen Minuten sind Sie auf der A7 oder mit dem Bus in Linz, gleichzeitig beginnen Felder und Wanderwege direkt hinter dem Grundstück. Schulen, Nahversorger und Ärzte finden Sie in Leonding selbst.

            Ein Lageplan und die Bebauungsbestimmungen stehen zum Download bereit – wir übermitteln Ihnen die Unterlagen gerne nach Ihrer Anfrage.
            """,

        ["Großes Baugrundstück Mühlviertel"] =
            """
            Günstiges Baugrundstück im schönen Mühlviertel: 1.200 m² am ruhigen Ortsrand von Freistadt, umgeben von Wiesen und mit freiem Blick in die typisch sanfte Hügellandschaft.

            Das Grundstück fällt leicht nach Südosten ab und eignet sich damit ideal für ein Haus mit Morgensonne in den Wohnräumen. Strom und Wasser liegen an der Grundgrenze, der Kanalanschluss ist von der Gemeinde bereits projektiert.

            * 1.200 m² Grundfläche, davon ca. 900 m² als Bauland gewidmet
            * Teilerschlossen (Strom, Wasser an der Grenze)
            * Ortsrandlage ohne Durchzugsverkehr
            * Freier Blick über die Mühlviertler Hügel

            Die Bezirksstadt Freistadt mit Schulen, Geschäften und der historischen Altstadt erreichen Sie in wenigen Minuten, die S10 bringt Sie in einer guten halben Stunde nach Linz. Ein leistbarer Startpunkt für Ihr Eigenheim im Grünen.
            """,

        ["Zwangsversteigerung: Haus in Traun"] =
            "Älteres Haus mit Renovierungsbedarf. Versteigerungstermin: nächsten Monat. Besichtigung möglich.",

        ["Zwangsversteigerung: Grundstück Enns"] =
            "Baugrundstück aus Zwangsversteigerung. Gute Lage, erschlossen.",

        ["Bungalow in Braunau"] =
            """
            Barrierefreier Bungalow in ruhiger Siedlungslage von Braunau am Inn – ideal für alle, die bequem auf einer Ebene wohnen möchten. Alle Räume sind stufenlos erreichbar, die Dusche ist bodengleich ausgeführt.

            Wohnzimmer, Küche und zwei Schlafzimmer gruppieren sich um den zentralen Flur; vom Wohnbereich gelangen Sie direkt auf die überdachte Terrasse. Der 600 m² große Garten ist pflegeleicht angelegt, ein Carport und ein Geräteraum sind vorhanden.

            HWB: 68 kWh/m²a. Auf Wunsch übernehmen wir die Vermittlung der bestehenden Gartenpflege.
            """,

        ["Doppelhaushälfte Vöcklabruck"] =
            """
            Neuwertige Doppelhaushälfte (Baujahr 2019) in familienfreundlicher Siedlungslage von Vöcklabruck. Schulen, Kindergarten und Spielplatz erreichen Sie zu Fuß – hier wachsen Kinder mit kurzen Wegen auf.

            Das Erdgeschoss überzeugt mit einem offenen Wohn-, Ess- und Küchenbereich samt Terrassenzugang; im Obergeschoss liegen drei Schlafzimmer und das Familienbad, darüber ein gedämmter Dachboden als Stauraum. Die Einbauküche aus 2020 bleibt im Kaufpreis.

            * Wärmepumpe mit Fußbodenheizung
            * Rollläden elektrisch, Fliegengitter auf allen Fenstern
            * Garage mit direktem Hauszugang
            * 280 m² Eigengarten mit Hochbeeten und Geräteraum

            Der Attersee und das Höllengebirge liegen für Ausflüge vor der Haustür, nach Vöcklabruck ins Zentrum sind es fünf Minuten, zur Autobahn A1 kaum mehr. Ein Zuhause zum Einziehen und Wohlfühlen – Besichtigungstermine vereinbaren wir gerne laufend, auch abends oder am Wochenende.

            HWB: 32 kWh/m²a, Klasse B.
            """,

        ["Einfamilienhaus am Traunfall"] =
            """
            Gepflegtes Einfamilienhaus in ruhiger Siedlungslage nahe dem Traunfall. Wer Wasserrauschen statt Straßenlärm sucht, findet hier ein Zuhause mit einem der schönsten Naturplätze Oberösterreichs in Gehweite.

            Das 2012 errichtete Haus bietet auf 140 m² einen klassischen Familiengrundriss: Wohn- und Essbereich mit Kachelofen im Erdgeschoss, drei Schlafzimmer und Bad im Obergeschoss, dazu ein Vollkeller mit Sauna-Vorbereitung. Die Terrasse liegt windgeschützt am Nachmittagssonnen-Eck des Hauses.

            * Ziegelmassivbau mit 50er-Ziegel, kein Vollwärmeschutz notwendig
            * Kachelofen mit Wärmespeicherung über Nacht
            * Garage plus zwei Stellplätze in der Einfahrt
            * 610 m² Garten mit altem Baumbestand und Gemüsebeet

            Roitham ist eine lebendige Gemeinde mit Nahversorger, Volksschule und aktivem Vereinsleben; Gmunden und Lambach erreichen Sie in je einer Viertelstunde. Über die A1-Anschlussstelle Steyrermühl sind Sie schnell in Linz oder Salzburg.

            HWB: 47 kWh/m²a. Übergabe kurzfristig möglich.
            """
    };
}
