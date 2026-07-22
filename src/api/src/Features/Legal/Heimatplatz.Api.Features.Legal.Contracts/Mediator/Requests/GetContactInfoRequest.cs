using Heimatplatz.Api.Features.Legal.Contracts.Models;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Legal.Contracts.Mediator.Requests;

/// <summary>
/// Request zum Abrufen der aufbereiteten Kontaktdaten (Impressum + Contact-Zusatzfelder).
/// Oeffentlich - das ist die Quelle fuer Web-Footer, Makler-Seite, JSON-LD und MAUI.
/// </summary>
public record GetContactInfoRequest : IRequest<GetContactInfoResponse>;

/// <summary>
/// Response mit den Kontaktdaten. Contact ist null, solange kein Impressum gepflegt ist.
/// </summary>
public record GetContactInfoResponse(ContactInfoDto? Contact);
