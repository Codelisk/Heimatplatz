using Heimatplatz.Api.Features.Dashboards.Contracts.Models;

namespace Heimatplatz.Api.Features.Dashboards.Services.Widgets;

/// <summary>
/// Der Feld-Katalog: welche WERTE eines Inserats die KI fuer Listen-Eintraege
/// und die Detail-Overlay auswaehlen darf ("Preis, Titel, m², ..."). Wie der
/// Widget-Katalog eine Quelle der Wahrheit im Code: der Prompt-Katalog wird
/// hieraus generiert, der Validator bereinigt fail-closed dagegen - beides kann
/// nie auseinanderlaufen. Neue Felder = ein Eintrag hier + Renderer je Frontend.
/// Alle Werte kommen aus den BESTEHENDEN DTOs (PropertyListItemDto fuer Listen,
/// PropertyDto fuer die Overlay) - der Katalog braucht keine neuen Endpoints.
/// </summary>
public static class DashboardFieldCatalog
{
    /// <param name="Key">Schluessel im Definition-JSON</param>
    /// <param name="InList">In Listen-Eintraegen verfuegbar (PropertyListItemDto)</param>
    /// <param name="InDetail">Im Fakten-Block der Detail-Overlay verfuegbar (PropertyDto)</param>
    /// <param name="PromptHint">Kurzbeschreibung fuer den KI-Katalog</param>
    public sealed record FieldSpec(string Key, bool InList, bool InDetail, string PromptHint);

    public static readonly IReadOnlyList<FieldSpec> All =
    [
        new("foto", InList: true, InDetail: false, "Vorschaubild (nur Listen)"),
        new("titel", InList: true, InDetail: true, "Inserats-Titel"),
        new("preis", InList: true, InDetail: true, "Kaufpreis"),
        new("preis-pro-m2", InList: false, InDetail: true, "Preis pro m² (nur Detail)"),
        new("typ", InList: true, InDetail: true, "Objektart-Badge (Haus/Grundstueck/Zwangsversteigerung)"),
        new("wohnflaeche", InList: true, InDetail: true, "Wohnflaeche in m²"),
        new("grundflaeche", InList: true, InDetail: true, "Grundflaeche in m²"),
        new("zimmer", InList: true, InDetail: true, "Zimmeranzahl"),
        new("ort", InList: true, InDetail: true, "PLZ + Ort"),
        new("strasse", InList: true, InDetail: true, "Strasse/Adresse"),
        new("anbieter", InList: true, InDetail: true, "Anbietername"),
        new("baujahr", InList: false, InDetail: true, "Baujahr (nur Detail)"),
        new("eingestellt-am", InList: true, InDetail: true, "Einstelldatum"),
        new("zv-termin", InList: true, InDetail: true, "Versteigerungstermin (nur Zwangsversteigerungen)"),
    ];

    /// <summary>Obergrenze je Listen-Eintrag - mehr passt in keine Zeile/Karte</summary>
    public const int MaxListFields = 6;

    /// <summary>Obergrenze im Fakten-Block der Overlay</summary>
    public const int MaxDetailFields = 12;

    public static readonly string[] AllowedSections =
    [
        DashboardDetailSections.Gallery,
        DashboardDetailSections.Facts,
        DashboardDetailSections.Description,
        DashboardDetailSections.Features,
        DashboardDetailSections.Contact,
        DashboardDetailSections.Map,
    ];

    /// <summary>
    /// Bereinigt eine KI-gelieferte Feldliste fail-closed: unbekannte oder im
    /// Kontext nicht verfuegbare Schluessel fliegen raus (Warnung), Duplikate
    /// werden entfernt, die Laenge gekappt. Leeres Ergebnis liefert null
    /// (= Preset/Standard uebernimmt).
    /// </summary>
    public static List<string>? NormalizeFields(IEnumerable<string?>? keys, bool forDetail, List<string> warnings)
    {
        if (keys is null)
            return null;

        var result = new List<string>();
        foreach (var raw in keys)
        {
            var key = raw?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(key) || result.Contains(key))
                continue;

            var spec = All.FirstOrDefault(f => f.Key == key);
            if (spec is null || (forDetail ? !spec.InDetail : !spec.InList))
            {
                warnings.Add($"Unbekanntes/unpassendes Feld verworfen: {raw}");
                continue;
            }
            result.Add(key);
        }

        var cap = forDetail ? MaxDetailFields : MaxListFields;
        if (result.Count > cap)
        {
            warnings.Add($"Feldliste auf {cap} Eintraege gekappt.");
            result = result.Take(cap).ToList();
        }

        return result.Count > 0 ? result : null;
    }

    /// <summary>Bereinigt die Detail-Spec (Sections + Felder); komplett leer = null (Standard-Aufbau).</summary>
    public static DashboardDetailSpec? NormalizeDetail(DashboardDetailSpec? detail, List<string> warnings)
    {
        if (detail is null)
            return null;

        var sections = detail.Sections?
            .Select(s => s?.Trim().ToLowerInvariant() ?? "")
            .Where(s => s.Length > 0)
            .Distinct()
            .Where(s =>
            {
                var known = AllowedSections.Contains(s);
                if (!known)
                    warnings.Add($"Unbekannte Detail-Sektion verworfen: {s}");
                return known;
            })
            .ToList();

        detail.Sections = sections is { Count: > 0 } ? sections : null;
        detail.Fields = NormalizeFields(detail.Fields, forDetail: true, warnings);

        return detail.Sections is null && detail.Fields is null ? null : detail;
    }
}
