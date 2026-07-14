namespace Heimatplatz.Maui.Features.Properties.Models;

/// <summary>
/// Sortieroptionen fuer die Immobilien-Liste
/// </summary>
public enum SortOption
{
    /// <summary>Neueste zuerst (Standard)</summary>
    Neueste,

    /// <summary>Aelteste zuerst</summary>
    Aelteste,

    /// <summary>Preis aufsteigend</summary>
    PreisAuf,

    /// <summary>Preis absteigend</summary>
    PreisAb,

    /// <summary>Grundstueck gross nach klein</summary>
    FlaecheAb,

    /// <summary>Grundstueck klein nach gross</summary>
    FlaecheAuf,

    /// <summary>PLZ aufsteigend</summary>
    PlzAuf,

    /// <summary>PLZ absteigend</summary>
    PlzAb
}
