using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;

/// <summary>
/// Laedt eine Immobilie fuer den Bearbeitungseditor.
/// Im Unterschied zum oeffentlichen Detail-Endpunkt ist dieser Request
/// authentifiziert und nur fuer den Eigentuemer zulaessig.
/// </summary>
public record GetEditablePropertyRequest(Guid Id) : IRequest<GetPropertyByIdResponse>;
