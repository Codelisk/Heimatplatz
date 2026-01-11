using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Immobilien.Contracts.Mediator.Requests;

/// <summary>
/// Request fuer Immobilien-Anzahl (Header: "OBJECTS: 14,202")
/// </summary>
public record GetImmobilienAnzahlRequest : IRequest<GetImmobilienAnzahlResponse>
{
    /// <summary>Optional: Filter nach Typ</summary>
    public ImmobilienTyp? Typ { get; init; }

    /// <summary>Status-Filter (Standard: nur aktive)</summary>
    public ImmobilienStatus Status { get; init; } = ImmobilienStatus.Aktiv;
}

/// <summary>
/// Response mit Anzahl der Immobilien
/// </summary>
public record GetImmobilienAnzahlResponse(int Anzahl);
