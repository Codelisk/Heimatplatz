namespace Heimatplatz.Api.Features.Immobilien.Contracts.Mediator.Requests;

/// <summary>
/// Klassifikation der Immobilie
/// </summary>
public enum ImmobilienTyp
{
    Haus = 1,
    Grundstueck = 2,
    Wohnung = 3
}

/// <summary>
/// Aktueller Status der Immobilie
/// </summary>
public enum ImmobilienStatus
{
    Aktiv = 1,
    Reserviert = 2,
    Verkauft = 3,
    Inaktiv = 4
}

/// <summary>
/// Sortieroptionen fuer Immobilien-Listen
/// </summary>
public enum ImmobilienSortierung
{
    ErstelltAm = 1,
    Preis = 2,
    Wohnflaeche = 3,
    Ort = 4
}

/// <summary>
/// Sortierrichtung
/// </summary>
public enum SortierRichtung
{
    Aufsteigend = 1,
    Absteigend = 2
}
