using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Admin.Contracts.Mediator.Requests;

/// <summary>
/// Backfill: geocodiert bestehende Inserate ohne Koordinaten (fuer die Kartenansicht).
/// Bewusst als manuell ausgeloester Admin-Endpoint statt Startup-Seeder - das Geocoding
/// ist auf 1 Request/Sekunde gedrosselt und wuerde den API-Start minutenlang blockieren.
/// Mehrfach aufrufen, bis Remaining 0 ist.
/// </summary>
public record GeocodeAdminPropertiesRequest(
    int Limit = 100
) : IRequest<GeocodeAdminPropertiesResponse>;

/// <param name="Processed">In diesem Lauf versuchte Inserate</param>
/// <param name="Geocoded">Davon erfolgreich mit Koordinaten versehen</param>
/// <param name="Failed">Davon ohne Treffer/Fehler (bleiben ohne Koordinaten)</param>
/// <param name="Remaining">Noch offene Inserate ohne Koordinaten (nach diesem Lauf)</param>
public record GeocodeAdminPropertiesResponse(
    int Processed,
    int Geocoded,
    int Failed,
    int Remaining
);
