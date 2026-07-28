using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Maui.Features.Properties.Models;
using Heimatplatz.Maui.Features.Properties.Services;
using Heimatplatz.Maui.Localization.Properties;
using Microsoft.Extensions.Logging;

namespace Heimatplatz.Maui.Features.Properties.Presentation;

/// <summary>
/// Gemeinsames Unter-ViewModel der Ort-Auswahl: Bezirk-&gt;Gemeinde-Panel (Akkordeon),
/// verdichtete Chip-Anzeige (<see cref="OrtChipBuilder"/>) und das zugeklappte Ort-Feld.
/// HomePage, Filtereinstellungen und Benachrichtigungen betten je ein Exemplar ein
/// (transient), binden es an OrtAuswahlPanel/OrtAuswahlFeld und reagieren nur noch
/// auf <see cref="SelectionApplied"/>.
/// </summary>
[Transient]
public partial class OrtAuswahlViewModel : ObservableObject
{
    private readonly ILocationService _locationService;
    private readonly ILogger<OrtAuswahlViewModel> _logger;
    private readonly OrtChipLabels _chipLabels;
    private readonly List<string> _selectedOrte = [];
    private Task? _treeLoadTask;

    public OrtAuswahlViewModel(
        ILocationService locationService,
        ILogger<OrtAuswahlViewModel> logger,
        OrtAuswahlStringsLocalized loc)
    {
        _locationService = locationService;
        _logger = logger;
        Loc = loc;

        _chipLabels = new OrtChipLabels(
            name => loc.BezirkAllFormat(name),
            (name, count) => loc.BezirkPartialFormat(name, count),
            count => loc.MoreChipsFormat(count));

        SearchText = string.Empty;
        SearchResults = [];
        ApplyText = loc.ApplyAllOrte;
    }

    /// <summary>Lokalisierte Texte fuer die geteilten Controls (Loc.Key)</summary>
    public OrtAuswahlStringsLocalized Loc { get; }

    public ObservableCollection<OrtBezirkItem> OrtBezirke { get; } = [];

    /// <summary>Verdichtete Chip-Anzeige der aktiven Auswahl</summary>
    public ObservableCollection<OrtChip> Chips { get; } = [];

    /// <summary>Aktive Auswahl (Gemeindenamen); Aenderung nur ueber SetSelection/Apply/RemoveChip</summary>
    public IReadOnlyList<string> SelectedOrte => _selectedOrte;

    /// <summary>Der Benutzer hat die Auswahl geaendert (Übernehmen oder Chip-✕)</summary>
    public event EventHandler? SelectionApplied;

    /// <summary>Arbeitskopie im offenen Panel geaendert - fuer Treffer-Vorschauen der Seiten</summary>
    public event EventHandler? PendingSelectionChanged;

    /// <summary>Aufgeklappter Bezirk (Index in <see cref="OrtBezirke"/>) soll an den Listenanfang</summary>
    public event EventHandler<int>? ScrollToBezirkRequested;

    [ObservableProperty]
    public partial bool IsOpen { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSearchActive))]
    [NotifyPropertyChangedFor(nameof(IsBrowseVisible))]
    public partial string SearchText { get; set; }

    [ObservableProperty]
    public partial List<OrtGemeindeItem> SearchResults { get; set; }

    /// <summary>Text des Übernehmen-Buttons; Seiten koennen ihn mit einer Treffer-Vorschau ueberschreiben</summary>
    [ObservableProperty]
    public partial string ApplyText { get; set; }

    /// <summary>True sobald im Panel gesucht wird - zeigt Suchergebnisse statt Bezirk-Liste</summary>
    public bool IsSearchActive => SearchText.Trim().Length >= 2;

    public bool IsBrowseVisible => !IsSearchActive;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    [NotifyPropertyChangedFor(nameof(FieldLabel))]
    public partial int SelectedCount { get; set; }

    public bool HasSelection => SelectedCount > 0;

    /// <summary>Beschriftung des zugeklappten Ort-Felds</summary>
    public string FieldLabel => SelectedCount switch
    {
        0 => Loc.FieldNone,
        1 => _selectedOrte[0],
        _ => Loc.FieldManyFormat(SelectedCount)
    };

    /// <summary>
    /// Ersetzt die Auswahl von aussen (FilterState/Preferences). Loest bewusst kein
    /// <see cref="SelectionApplied"/> aus - sonst wuerden Sync-Pfade Speicher-Schleifen bauen.
    /// </summary>
    public void SetSelection(IEnumerable<string> orte)
    {
        _selectedOrte.Clear();
        _selectedOrte.AddRange(orte);
        SelectedCount = _selectedOrte.Count;
        RebuildChips();
    }

    /// <summary>Aktuelle Arbeitskopie im Panel - fuer Treffer-Vorschauen der Seiten</summary>
    public List<string> GetPendingSelection()
        => OrtBezirke
            .SelectMany(b => b.Gemeinden)
            .Where(g => g.IsSelected)
            .Select(g => g.Name)
            .ToList();

    /// <summary>
    /// Baut den Bezirk-&gt;Gemeinde-Baum genau einmal auf (single-flight). Seiten mit
    /// Chip-Anzeige rufen das schon in OnAppearing - ohne Baum zerfaellt eine
    /// Bezirksauswahl in einen Chip je Gemeinde.
    /// </summary>
    public Task EnsureTreeAsync()
    {
        if (OrtBezirke.Count > 0)
            return Task.CompletedTask;
        if (_treeLoadTask is { IsCompleted: false })
            return _treeLoadTask;

        _treeLoadTask = BuildTreeAsync();
        return _treeLoadTask;
    }

    private async Task BuildTreeAsync()
    {
        List<LocationBezirkDto> quelle;
        try
        {
            var locations = await _locationService.GetLocationsAsync();
            quelle = locations.SelectMany(bl => bl.Bezirke).ToList();
        }
        catch (Exception ex)
        {
            // Laeuft auch im Hintergrund (OnAppearing) - ein Fehlschlag darf die Seite
            // nicht mitreissen; EnsureTreeAsync versucht es beim naechsten Aufruf erneut.
            _logger.LogWarning(ex, "[OrtAuswahl] Ortsliste konnte nicht geladen werden");
            return;
        }

        var bezirke = quelle
            .OrderBy(bz => bz.Name, StringComparer.CurrentCulture)
            .Select(bz =>
            {
                var gemeinden = bz.Gemeinden
                    .OrderBy(g => g.Name, StringComparer.CurrentCulture)
                    .Select(g => new OrtGemeindeItem { Name = g.Name, PostalCode = g.PostalCode })
                    .ToList();
                var bezirk = new OrtBezirkItem
                {
                    Name = bz.Name,
                    Gemeinden = gemeinden,
                    CountLabelFormatter = count => Loc.SelectedCountFormat(count)
                };
                foreach (var gemeinde in gemeinden)
                    gemeinde.Bezirk = bezirk;
                return bezirk;
            });

        OrtBezirke.Clear();
        foreach (var bezirk in bezirke)
            OrtBezirke.Add(bezirk);

        // Chips neu gruppieren - gespeicherte Orte koennen vor dem Baum angekommen sein
        RebuildChips();
    }

    [RelayCommand]
    private async Task OpenAsync()
    {
        await EnsureTreeAsync();
        SearchText = string.Empty;
        SearchResults = [];
        SyncPanelFromSelection();
        OnPendingChanged();
        IsOpen = true;
    }

    /// <summary>Tap auf den Chip-Text: oeffnet das Panel beim Bezirk hinter dem Chip</summary>
    [RelayCommand]
    private async Task OpenChipAsync(OrtChip chip)
    {
        await OpenAsync();
        if (chip.Bezirk is { } bezirk && OrtBezirke.Contains(bezirk))
            ExpandOnly(bezirk);
    }

    /// <summary>
    /// Uebertraegt die aktive Auswahl in die Arbeitskopie des Panels. Akkordeon:
    /// hoechstens ein Bezirk startet offen (der erste nur teilweise gewaehlte),
    /// sonst summieren sich mehrere aufgeklappte Bezirke zu hunderten Zeilen.
    /// </summary>
    private void SyncPanelFromSelection()
    {
        var selected = _selectedOrte.ToHashSet();
        OrtBezirkItem? firstPartial = null;

        foreach (var bezirk in OrtBezirke)
        {
            foreach (var gemeinde in bezirk.Gemeinden)
                gemeinde.IsSelected = selected.Contains(gemeinde.Name);
            bezirk.RefreshSelectedCount();
            bezirk.IsExpanded = false;

            if (firstPartial is null && bezirk.HasSelection && !bezirk.IsAllSelected)
                firstPartial = bezirk;
        }

        if (firstPartial is not null)
            firstPartial.IsExpanded = true;
    }

    /// <summary>
    /// Klappt genau einen Bezirk auf (oder alle zu) und meldet ihn der Seite zum
    /// Hochscrollen - aufgeklappt unterhalb des Sichtbereichs sieht ihn sonst niemand.
    /// </summary>
    private void ExpandOnly(OrtBezirkItem? target)
    {
        foreach (var bezirk in OrtBezirke)
            bezirk.IsExpanded = ReferenceEquals(bezirk, target);

        if (target is not null)
            ScrollToBezirkRequested?.Invoke(this, OrtBezirke.IndexOf(target));
    }

    partial void OnSearchTextChanged(string value)
    {
        var search = value.Trim();
        if (search.Length < 2)
        {
            SearchResults = [];
            return;
        }

        SearchResults = OrtBezirke
            .SelectMany(b => b.Gemeinden)
            .Where(g => g.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                     || g.PostalCode.StartsWith(search, StringComparison.OrdinalIgnoreCase))
            .Take(30)
            .ToList();
    }

    [RelayCommand]
    private void ToggleBezirkExpanded(OrtBezirkItem bezirk)
        => ExpandOnly(bezirk.IsExpanded ? null : bezirk);

    /// <summary>Sammel-Checkbox: waehlt alle Gemeinden eines Bezirks an bzw. ab</summary>
    [RelayCommand]
    private void ToggleBezirkSelection(OrtBezirkItem bezirk)
    {
        var target = !bezirk.IsAllSelected;
        foreach (var gemeinde in bezirk.Gemeinden)
            gemeinde.IsSelected = target;
        bezirk.RefreshSelectedCount();
        OnPendingChanged();
    }

    [RelayCommand]
    private void ToggleOrtGemeinde(OrtGemeindeItem gemeinde)
    {
        gemeinde.IsSelected = !gemeinde.IsSelected;
        gemeinde.Bezirk?.RefreshSelectedCount();
        OnPendingChanged();
    }

    [RelayCommand]
    private void Reset()
    {
        foreach (var bezirk in OrtBezirke)
        {
            foreach (var gemeinde in bezirk.Gemeinden)
                gemeinde.IsSelected = false;
            bezirk.SelectedCount = 0;
        }
        OnPendingChanged();
    }

    /// <summary>Uebernimmt die Arbeitskopie in die Auswahl und meldet SelectionApplied</summary>
    [RelayCommand]
    private void Apply()
    {
        SetSelection(GetPendingSelection());
        IsOpen = false;
        SelectionApplied?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Entfernt einen Chip (einzelner Ort oder ganze Bezirks-Gruppe)</summary>
    [RelayCommand]
    private void RemoveChip(OrtChip chip)
    {
        var toRemove = chip.Orte.ToHashSet();
        SetSelection(_selectedOrte.Where(o => !toRemove.Contains(o)).ToList());
        SelectionApplied?.Invoke(this, EventArgs.Empty);
    }

    private void OnPendingChanged()
    {
        var count = OrtBezirke.Sum(b => b.Gemeinden.Count(g => g.IsSelected));
        ApplyText = count switch
        {
            0 => Loc.ApplyAllOrte,
            1 => Loc.ApplyOneOrt,
            _ => Loc.ApplyManyOrteFormat(count)
        };
        PendingSelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RebuildChips()
    {
        Chips.Clear();
        foreach (var chip in OrtChipBuilder.Build(OrtBezirke, _selectedOrte, _chipLabels))
            Chips.Add(chip);
    }
}
