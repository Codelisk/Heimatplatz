using CommunityToolkit.Mvvm.ComponentModel;

namespace Heimatplatz.Maui.Features.Properties.Models;

/// <summary>
/// Gemeinde-Zeile im Ort-Auswahl-Panel. IsSelected ist eine Arbeitskopie
/// der Auswahl, die erst mit "Übernehmen" in den Filter uebernommen wird.
/// </summary>
public partial class OrtGemeindeItem : ObservableObject
{
    public required string Name { get; init; }
    public required string PostalCode { get; init; }

    /// <summary>Rueckreferenz auf die Bezirk-Gruppe (nach Konstruktion gesetzt)</summary>
    public OrtBezirkItem? Bezirk { get; internal set; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    /// <summary>
    /// Name mit PLZ - nur fuer die Suchergebnis-Liste, wo der blosse Name ohne
    /// Bezirk-Kontext mehrdeutig waere. Im aufgeklappten Bezirk steht der Name allein,
    /// sonst passen die Zeilen auf breiten Fenstern nicht in zwei Spalten.
    /// </summary>
    public string DisplayName => $"{Name} ({PostalCode})";
    public string BezirkName => Bezirk?.Name ?? string.Empty;
}

/// <summary>
/// Aufklappbare Bezirk-Gruppe im Ort-Auswahl-Panel mit Sammelauswahl
/// (alle Gemeinden des Bezirks) und Auswahl-Zaehler.
/// </summary>
public partial class OrtBezirkItem : ObservableObject
{
    public required string Name { get; init; }
    public required IReadOnlyList<OrtGemeindeItem> Gemeinden { get; init; }

    /// <summary>
    /// Lokalisierter Zaehler-Text ("{0} ausgewählt") - liefert das konstruierende
    /// ViewModel (Home bzw. FilterSettings) mit, Models haben kein DI/Loc.
    /// </summary>
    public required Func<int, string> CountLabelFormatter { get; init; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExpandGlyph))]
    [NotifyPropertyChangedFor(nameof(VisibleGemeinden))]
    public partial bool IsExpanded { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAllSelected))]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    [NotifyPropertyChangedFor(nameof(CheckGlyph))]
    [NotifyPropertyChangedFor(nameof(CountLabel))]
    public partial int SelectedCount { get; set; }

    public bool IsAllSelected => Gemeinden.Count > 0 && SelectedCount == Gemeinden.Count;
    public bool HasSelection => SelectedCount > 0;

    /// <summary>Tri-State-Glyph: ✓ = alle, – = teilweise, leer = keine</summary>
    public string CheckGlyph => IsAllSelected ? "✓" : HasSelection ? "–" : string.Empty;
    public string CountLabel => HasSelection ? CountLabelFormatter(SelectedCount) : string.Empty;
    public string ExpandGlyph => IsExpanded ? "▾" : "▸";

    /// <summary>
    /// Gemeinden erst an das BindableLayout geben, wenn der Bezirk wirklich offen ist.
    /// Andernfalls materialisiert MAUI alle Gemeinde-Views bereits im geschlossenen
    /// Bottom Sheet und blockiert die Homepage beim ersten Aufbau mehrere Sekunden.
    /// </summary>
    public IReadOnlyList<OrtGemeindeItem> VisibleGemeinden
        => IsExpanded ? Gemeinden : [];

    public void RefreshSelectedCount()
        => SelectedCount = Gemeinden.Count(g => g.IsSelected);
}

/// <summary>
/// Anzeige-Chip fuer die aktive Ort-Auswahl in der Filterleiste. Ein Chip steht
/// entweder fuer einen ganz oder groesstenteils gewaehlten Bezirk, fuer einen
/// einzelnen Ort oder - als Ueberlauf - fuer den nicht mehr angezeigten Rest.
/// <see cref="Orte"/> enthaelt immer alle Gemeindenamen hinter dem Chip.
/// </summary>
public sealed record OrtChip(
    string Label,
    IReadOnlyList<string> Orte,
    OrtBezirkItem? Bezirk = null,
    bool IsOverflow = false)
{
    /// <summary>Der Ueberlauf-Chip fuehrt nur ins Panel und traegt daher kein ✕.</summary>
    public bool CanRemove => !IsOverflow;
}

/// <summary>
/// Lokalisierte Chip-Beschriftungen. Die ViewModels reichen ihre generierte
/// Loc-Klasse durch, damit der Builder ohne DI/Localization auskommt.
/// </summary>
public sealed record OrtChipLabels(
    Func<string, string> BezirkAll,
    Func<string, int, string> BezirkPartial,
    Func<int, string> More);

/// <summary>
/// Verdichtet die flache Ortsauswahl (Liste von Gemeindenamen) zu wenigen Chips.
/// Ohne diese Verdichtung wird ein per Sammel-Checkbox gewaehlter Bezirk zu einem
/// Chip je Gemeinde - Voecklabruck allein sind ueber 50 Stueck, die die Filterseite
/// zuscrollen. Deshalb: ganzer Bezirk = ein Chip, ab <see cref="GroupThreshold"/>
/// gewaehlten Gemeinden ebenfalls ein Bezirks-Chip, und insgesamt nie mehr als
/// <see cref="MaxVisibleChips"/> Chips.
/// </summary>
public static class OrtChipBuilder
{
    /// <summary>Ab so vielen Gemeinden desselben Bezirks wird zusammengefasst.</summary>
    public const int GroupThreshold = 3;

    /// <summary>Mehr als zwei Chip-Zeilen sollen es nicht werden.</summary>
    public const int MaxVisibleChips = 6;

    public static IReadOnlyList<OrtChip> Build(
        IReadOnlyList<OrtBezirkItem> bezirke,
        IReadOnlyList<string> selectedOrte,
        OrtChipLabels labels)
    {
        if (selectedOrte.Count == 0)
            return [];

        var chips = new List<OrtChip>();
        var remaining = new HashSet<string>(selectedOrte);
        var bezirkByOrt = new Dictionary<string, OrtBezirkItem>();

        foreach (var bezirk in bezirke)
        {
            foreach (var gemeinde in bezirk.Gemeinden)
                bezirkByOrt.TryAdd(gemeinde.Name, bezirk);

            var treffer = bezirk.Gemeinden.Where(g => remaining.Contains(g.Name)).ToList();
            if (treffer.Count == 0)
                continue;

            var isAll = treffer.Count == bezirk.Gemeinden.Count;
            if (!isAll && treffer.Count < GroupThreshold)
                continue; // ein oder zwei Orte sagen als Klartext-Chip mehr aus

            chips.Add(new OrtChip(
                isAll ? labels.BezirkAll(bezirk.Name) : labels.BezirkPartial(bezirk.Name, treffer.Count),
                treffer.Select(g => g.Name).ToList(),
                bezirk));

            foreach (var gemeinde in treffer)
                remaining.Remove(gemeinde.Name);
        }

        // Einzelne Orte in Auswahlreihenfolge. Solange der Bezirk-Baum noch nicht
        // geladen ist, laeuft die gesamte Auswahl hier durch - der Deckel unten
        // haelt die Anzeige auch dann kurz.
        foreach (var ort in selectedOrte)
        {
            if (remaining.Remove(ort))
                chips.Add(new OrtChip(ort, [ort], bezirkByOrt.GetValueOrDefault(ort)));
        }

        if (chips.Count <= MaxVisibleChips)
            return chips;

        var overflow = chips.Skip(MaxVisibleChips - 1).ToList();
        var visible = chips.Take(MaxVisibleChips - 1).ToList();
        visible.Add(new OrtChip(
            labels.More(overflow.Count),
            overflow.SelectMany(chip => chip.Orte).ToList(),
            Bezirk: null,
            IsOverflow: true));
        return visible;
    }
}
