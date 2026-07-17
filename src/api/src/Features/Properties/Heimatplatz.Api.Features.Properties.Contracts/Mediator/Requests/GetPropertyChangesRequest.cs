using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;

/// <summary>
/// Delta-Sync fuer Client-Caches: liefert alle Immobilien-Aenderungen seit
/// <paramref name="Since"/> (dedupliziert pro Immobilie, inkl. aktueller Listendaten).
/// Ohne <paramref name="Since"/> oder bei zu altem Stand wird ein Voll-Refresh signalisiert.
/// </summary>
/// <param name="Since">
/// Wasserzeichen des letzten Syncs als ISO-8601-Roundtrip-String ("O").
/// Bewusst string statt DateTimeOffset: der generierte Shiny-HTTP-Client serialisiert
/// DateTimeOffset-Query-Parameter kulturabhaengig (z.B. "17/07/2026 05:56:46 +00:00"),
/// was serverseitig nicht bindbar ist.
/// </param>
public record GetPropertyChangesRequest(
    string? Since = null
) : IRequest<GetPropertyChangesResponse>;

/// <summary>
/// Response fuer GetPropertyChangesRequest
/// </summary>
public record GetPropertyChangesResponse
{
    /// <summary>Wasserzeichen fuer den naechsten Sync-Aufruf (als neues Since verwenden)</summary>
    public required DateTimeOffset Watermark { get; init; }

    /// <summary>
    /// True wenn kein Delta geliefert werden kann (Since fehlt oder liegt ausserhalb
    /// der Journal-Aufbewahrung) - der Client muss seine Immobilien-Caches komplett erneuern
    /// </summary>
    public required bool FullResyncRequired { get; init; }

    /// <summary>Aenderungen seit Since, dedupliziert pro Immobilie (leer bei FullResyncRequired)</summary>
    public required List<PropertyChangeDto> Changes { get; init; }
}

/// <summary>
/// Eine Immobilien-Aenderung fuer den Delta-Sync
/// </summary>
public record PropertyChangeDto
{
    /// <summary>Id der betroffenen Immobilie</summary>
    public required Guid PropertyId { get; init; }

    /// <summary>Art der Aenderung: Created, Updated, Deleted</summary>
    public required string ChangeType { get; init; }

    /// <summary>Zeitpunkt der (letzten) Aenderung</summary>
    public required DateTimeOffset ChangedAt { get; init; }

    /// <summary>Aktuelle Listendaten der Immobilie (null bei Deleted)</summary>
    public PropertyListItemDto? Property { get; init; }
}
