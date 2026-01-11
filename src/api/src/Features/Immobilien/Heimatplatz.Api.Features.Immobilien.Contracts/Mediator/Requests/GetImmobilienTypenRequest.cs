using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Immobilien.Contracts.Mediator.Requests;

/// <summary>
/// Request fuer verfuegbare Immobilientypen (fuer Filter-Dropdown)
/// </summary>
public record GetImmobilienTypenRequest : IRequest<GetImmobilienTypenResponse>;

/// <summary>
/// Response mit verfuegbaren Immobilientypen
/// </summary>
public record GetImmobilienTypenResponse(IReadOnlyList<ImmobilienTypDto> Typen);

/// <summary>
/// DTO fuer Immobilientyp
/// </summary>
public record ImmobilienTypDto(int Wert, string Name, string Anzeigename);
