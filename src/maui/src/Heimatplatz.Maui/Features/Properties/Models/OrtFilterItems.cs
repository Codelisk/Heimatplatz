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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExpandGlyph))]
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
    public string CountLabel => HasSelection ? $"{SelectedCount} ausgewählt" : string.Empty;
    public string ExpandGlyph => IsExpanded ? "▾" : "▸";

    public void RefreshSelectedCount()
        => SelectedCount = Gemeinden.Count(g => g.IsSelected);
}

/// <summary>
/// Anzeige-Chip fuer die aktive Ort-Auswahl in der Filterleiste.
/// Ein komplett gewaehlter Bezirk wird zu einem Chip "{Bezirk} (alle)"
/// zusammengefasst; Orte enthaelt dann alle Gemeindenamen des Bezirks.
/// </summary>
public sealed record OrtChip(string Label, IReadOnlyList<string> Orte);
