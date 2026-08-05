namespace Heimatplatz.Maui.Features.Properties.Services;

/// <summary>
/// Leporello-Falz fuer lange Beschreibungen - C#-Portierung der Web-Logik
/// (src/web/src/features/properties/description-blocks.ts, planDescriptionFold).
///
/// Teilt eine Beschreibung in sichtbaren Vorspann (Lead) und zusammengefalteten
/// Rest. Der Schnitt faellt immer auf eine Absatz-, Zeilen- oder Satzgrenze -
/// nie mitten in einen Satz. Kurze Texte bleiben ungefaltet (RestText = null).
/// Schwellwerte identisch zum Web, damit beide Frontends gleich falten.
///
/// Fuer die Anzeige werden ASCII-Aufzaehlungen der Feeds ("* Doppelgarage",
/// "• 3 Zimmer") in Bullet-Zeilen umgeschrieben und Absaetze mit Leerzeile
/// getrennt - Vorspann und Rest sind je EIN fertiger Label-Text. Bewusst keine
/// Block-Views pro Absatz: ein Label pro Teil ist ein einziger Layout-Pass,
/// auch bei OpenImmo-Texten mit 30+ Mini-Bloecken (Performance vor Optik).
/// </summary>
public static class DescriptionFoldPlanner
{
    private const int FoldMinTotal = 900; // darunter lohnt sich kein Falz
    private const int LeadBudget = 500; // Ziellaenge des sichtbaren Vorspanns in Zeichen
    private const int MinRest = 300; // kleinere Reste einfach mit anzeigen
    private const int SplitSlack = 350; // max. Abstand hinter dem Budget fuer eine Trennstelle
    private const int ReadingCharsPerMinute = 1250; // durchschnittliches Lesetempo Deutsch

    /// <param name="LeadText">Sichtbarer Vorspann (bzw. der ganze Text, wenn ungefaltet)</param>
    /// <param name="RestText">Zusammengefalteter Rest; null = kein Falz</param>
    /// <param name="ReadingMinutes">Geschaetzte Lesezeit des Rests in Minuten (min. 1)</param>
    public sealed record DescriptionFoldPlan(string? LeadText, string? RestText, int ReadingMinutes)
    {
        public bool IsFolded => RestText != null;
    }

    public static DescriptionFoldPlan Plan(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new DescriptionFoldPlan(null, null, 0);

        var blocks = SplitBlocks(text);
        var total = blocks.Sum(b => b.Text.Length);
        if (total < FoldMinTotal)
            return Unfolded(blocks);

        var lead = new List<Block>();
        var rest = new List<Block>();
        var seen = 0;

        foreach (var block in blocks)
        {
            if (seen >= LeadBudget)
            {
                rest.Add(block);
                continue;
            }

            // Ein Absatz, der das Budget weit ueberschiesst, wird an einer
            // natuerlichen Grenze geteilt statt komplett sichtbar zu bleiben
            if (!block.IsList && seen + block.Text.Length > LeadBudget + SplitSlack)
            {
                var cut = FindParagraphCut(block.Text, LeadBudget - seen);
                if (cut != null)
                {
                    lead.Add(new Block(cut.Value.Head, IsList: false));
                    rest.Add(new Block(cut.Value.Tail, IsList: false));
                    seen = LeadBudget;
                    continue;
                }
            }

            lead.Add(block);
            seen += block.Text.Length;
        }

        var restTotal = rest.Sum(b => b.Text.Length);
        if (restTotal < MinRest)
            return Unfolded(blocks);

        var minutes = Math.Max(1, (int)Math.Round(restTotal / (double)ReadingCharsPerMinute, MidpointRounding.AwayFromZero));
        return new DescriptionFoldPlan(Join(lead), Join(rest), minutes);
    }

    private readonly record struct Block(string Text, bool IsList);

    private static DescriptionFoldPlan Unfolded(List<Block> blocks) =>
        new(Join(blocks), null, 0);

    private static string Join(List<Block> blocks) =>
        string.Join("\n\n", blocks.Select(b => b.Text));

    /// <summary>
    /// Segmentiert Plaintext in Absatz- und Aufzaehlungsbloecke (Leerzeile trennt,
    /// "*"/"•"-Zeilen werden zu Bullet-Zeilen) - Gegenstueck zu splitDescriptionBlocks.
    /// </summary>
    private static List<Block> SplitBlocks(string text)
    {
        var blocks = new List<Block>();
        var paragraphLines = new List<string>();
        var listLines = new List<string>();

        void FlushParagraph()
        {
            if (paragraphLines.Count > 0)
            {
                blocks.Add(new Block(string.Join("\n", paragraphLines), IsList: false));
                paragraphLines.Clear();
            }
        }

        void FlushList()
        {
            if (listLines.Count > 0)
            {
                blocks.Add(new Block(string.Join("\n", listLines), IsList: true));
                listLines.Clear();
            }
        }

        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r').Trim();
            if (line.Length == 0)
            {
                FlushParagraph();
                FlushList();
                continue;
            }

            if (line.Length > 2 && line[0] is '*' or '•' && char.IsWhiteSpace(line[1]))
            {
                FlushParagraph();
                listLines.Add("•  " + line[1..].Trim());
                continue;
            }

            FlushList();
            paragraphLines.Add(line);
        }

        FlushParagraph();
        FlushList();
        return blocks;
    }

    /// <summary>Trennt einen langen Absatz an einer Zeilen- oder Satzgrenze nahe dem Budget.</summary>
    private static (string Head, string Tail)? FindParagraphCut(string text, int budget)
    {
        var from = Math.Max(0, budget);
        foreach (var marker in new[] { "\n", ". " })
        {
            var idx = from < text.Length ? text.IndexOf(marker, from, StringComparison.Ordinal) : -1;
            if (idx != -1 && idx <= budget + SplitSlack)
            {
                var cutAt = marker == ". " ? idx + 1 : idx; // Satzpunkt bleibt beim Vorspann
                var head = text[..cutAt].TrimEnd();
                var tail = text[cutAt..].TrimStart();
                if (head.Length > 0 && tail.Length > 0)
                    return (head, tail);
            }
        }

        return null;
    }
}
