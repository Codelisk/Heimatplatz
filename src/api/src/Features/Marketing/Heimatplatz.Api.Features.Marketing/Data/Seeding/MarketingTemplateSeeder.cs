using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Core.Data.Seeding;
using Heimatplatz.Api.Features.Marketing.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Heimatplatz.Api.Features.Marketing.Data.Seeding;

/// <summary>
/// Start-Vorlagen fuer den E-Mail-Versand. Referenzdaten (IsDemoData=false): ohne mindestens
/// eine Vorlage waere die Auswahl auf der Schreiben-Seite leer.
/// Idempotent pro Name - eigene Vorlagen und Aenderungen an den Start-Vorlagen bleiben
/// bei jedem Start erhalten, fehlende werden ergaenzt.
/// </summary>
public class MarketingTemplateSeeder(AppDbContext dbContext) : ISeeder
{
    public int Order => 21;

    public bool IsDemoData => false;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var set = dbContext.Set<MarketingEmailTemplate>();
        var existingNames = await set
            .Select(x => x.Name)
            .ToListAsync(cancellationToken);

        var missing = Defaults()
            .Where(x => !existingNames.Contains(x.Name))
            .ToList();

        if (missing.Count == 0)
            return;

        set.AddRange(missing);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static List<MarketingEmailTemplate> Defaults() =>
    [
        new MarketingEmailTemplate
        {
            Name = "Erstkontakt",
            Description = "Kurze Vorstellung von Heimatplatz, wenn noch kein Gespraech stattgefunden hat.",
            DisplayOrder = 10,
            Subject = "Ihre Immobilien auf Heimatplatz",
            Body =
                """
                {anrede},

                mein Name ist Daniel Hufnagl, ich betreibe Heimatplatz - die regionale
                Immobilienplattform fuer Oberoesterreich.

                Wir buendeln Angebote aus der Region an einem Ort, damit Suchende nicht
                mehr ueber mehrere Portale hinweg vergleichen muessen. Fuer Anbieter wie
                {firma} bedeutet das zusaetzliche Sichtbarkeit genau bei den Menschen,
                die in der Umgebung suchen.

                Haetten Sie Interesse, Ihre Objekte auf Heimatplatz zu praesentieren?
                Ich zeige Ihnen die Plattform gerne in einem kurzen Telefonat.
                """
        },
        new MarketingEmailTemplate
        {
            Name = "Follow-up nach Telefonat",
            Description = "Nachfassen im Anschluss an ein Gespraech, mit Verweis auf das Telefonat.",
            DisplayOrder = 20,
            Subject = "Unser Telefonat - Heimatplatz fuer {firma}",
            Body =
                """
                {anrede},

                vielen Dank fuer das freundliche Gespraech.

                Wie besprochen fasse ich kurz zusammen: Heimatplatz ist die regionale
                Immobilienplattform fuer Oberoesterreich. Ihre Objekte erscheinen dort
                gebuendelt neben den anderen Angeboten aus {ort} und Umgebung.

                Melden Sie sich einfach, wenn Sie starten moechten - ich richte Ihnen den
                Zugang ein und uebernehme die ersten Inserate gerne gemeinsam mit Ihnen.
                """
        }
    ];
}
