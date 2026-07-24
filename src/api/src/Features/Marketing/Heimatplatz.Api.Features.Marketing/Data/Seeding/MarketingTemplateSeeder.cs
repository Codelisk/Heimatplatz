using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Core.Data.Seeding;
using Heimatplatz.Api.Features.Marketing.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Heimatplatz.Api.Features.Marketing.Data.Seeding;

/// <summary>
/// Start-Vorlagen fuer den E-Mail-Versand. Referenzdaten (IsDemoData=false): ohne mindestens
/// eine Vorlage waere die Auswahl auf der Schreiben-Seite anfangs leer.
/// Laeuft nur einmal (solange die Tabelle leer ist) - danach sind die Vorlagen
/// nutzergepflegt: eine bewusst geloeschte Start-Vorlage bleibt geloescht und wird beim
/// naechsten Start NICHT wieder angelegt. Auf einer komplett geleerten Tabelle werden die
/// Start-Vorlagen bewusst neu angeboten.
/// </summary>
public class MarketingTemplateSeeder(AppDbContext dbContext) : ISeeder
{
    public int Order => 21;

    public bool IsDemoData => false;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var set = dbContext.Set<MarketingEmailTemplate>();
        if (await set.AnyAsync(cancellationToken))
            return;

        set.AddRange(Defaults());
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static List<MarketingEmailTemplate> Defaults() =>
    [
        new MarketingEmailTemplate
        {
            Name = "Erstkontakt",
            Description = "Kurze Vorstellung von Heimatplatz, wenn noch kein Gespräch stattgefunden hat.",
            DisplayOrder = 10,
            Subject = "Ihre Immobilien auf Heimatplatz",
            Body =
                """
                {anrede},

                mein Name ist Daniel Hufnagl, ich betreibe Heimatplatz - die regionale
                Immobilienplattform für Oberösterreich.

                Wir bündeln Angebote aus der Region an einem Ort, damit Suchende nicht
                mehr über mehrere Portale hinweg vergleichen müssen. Für Anbieter wie
                {firma} bedeutet das zusätzliche Sichtbarkeit genau bei den Menschen,
                die in der Umgebung suchen.

                Hätten Sie Interesse, Ihre Objekte auf Heimatplatz zu präsentieren?
                Ich zeige Ihnen die Plattform gerne in einem kurzen Telefonat.
                """
        },
        new MarketingEmailTemplate
        {
            Name = "Follow-up nach Telefonat",
            Description = "Nachfassen im Anschluss an ein Gespräch, mit Verweis auf das Telefonat.",
            DisplayOrder = 20,
            Subject = "Unser Telefonat - Heimatplatz für {firma}",
            Body =
                """
                {anrede},

                vielen Dank für das freundliche Gespräch.

                Wie besprochen fasse ich kurz zusammen: Heimatplatz ist die regionale
                Immobilienplattform für Oberösterreich. Ihre Objekte erscheinen dort
                gebündelt neben den anderen Angeboten aus {ort} und Umgebung.

                Melden Sie sich einfach, wenn Sie starten möchten - ich richte Ihnen den
                Zugang ein und übernehme die ersten Inserate gerne gemeinsam mit Ihnen.
                """
        }
    ];
}
