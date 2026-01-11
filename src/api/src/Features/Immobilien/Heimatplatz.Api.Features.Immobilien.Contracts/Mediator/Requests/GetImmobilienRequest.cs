using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Immobilien.Contracts.Mediator.Requests;

/// <summary>
/// Request fuer paginierte Immobilien-Liste mit Filtern
/// </summary>
public record GetImmobilienRequest : IRequest<GetImmobilienResponse>
{
    /// <summary>Filter nach Immobilientyp (Haus, Grundstueck, Wohnung)</summary>
    public ImmobilienTyp? Typ { get; init; }

    /// <summary>Minimaler Preis in EUR</summary>
    public decimal? MinPreis { get; init; }

    /// <summary>Maximaler Preis in EUR</summary>
    public decimal? MaxPreis { get; init; }

    /// <summary>Minimale Wohnflaeche in m²</summary>
    public decimal? MinWohnflaeche { get; init; }

    /// <summary>Suche nach Ort, Bezirk oder Region</summary>
    public string? OrtSuche { get; init; }

    /// <summary>Seitennummer (1-basiert)</summary>
    public int Seite { get; init; } = 1;

    /// <summary>Eintraege pro Seite</summary>
    public int SeitenGroesse { get; init; } = 20;

    /// <summary>Sortierfeld</summary>
    public ImmobilienSortierung Sortierung { get; init; } = ImmobilienSortierung.ErstelltAm;

    /// <summary>Sortierrichtung</summary>
    public SortierRichtung Richtung { get; init; } = SortierRichtung.Absteigend;
}

/// <summary>
/// Response mit paginierten Immobilien
/// </summary>
public record GetImmobilienResponse(
    IReadOnlyList<ImmobilieListeDto> Eintraege,
    int GesamtAnzahl,
    int Seite,
    int SeitenGroesse,
    int GesamtSeiten
);

/// <summary>
/// DTO fuer Immobilie in Listenansicht (Card)
/// </summary>
public record ImmobilieListeDto(
    Guid Id,
    string Titel,
    ImmobilienTyp Typ,
    decimal Preis,
    string Waehrung,
    decimal Wohnflaeche,
    decimal? Grundstuecksflaeche,
    string Ort,
    string? Bezirk,
    string? HauptbildUrl,
    string? ZusatzInfo
);
