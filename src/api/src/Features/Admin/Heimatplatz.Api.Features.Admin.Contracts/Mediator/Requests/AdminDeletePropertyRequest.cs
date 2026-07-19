using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Admin.Contracts.Mediator.Requests;

/// <summary>
/// Endgueltiges Loeschen eines Inserats durch den Admin (ohne Ownership-Pruefung,
/// inkl. hochgeladener Bilddateien). Fuer Moderation gedacht - zum blossen
/// Verstecken <see cref="SetPropertyVisibilityRequest"/> verwenden.
/// </summary>
public record AdminDeletePropertyRequest(Guid Id) : IRequest<AdminDeletePropertyResponse>;

public record AdminDeletePropertyResponse(
    bool Success,
    string Message
);
