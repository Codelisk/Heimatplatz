using Heimatplatz.Api.Features.Marketing.Contracts.Models;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;

/// <summary>
/// Kontakt anlegen (Id=null) oder bearbeiten (Id gesetzt). Die E-Mail-Adresse wird
/// serverseitig normalisiert (lowercase/trim); Dubletten liefern Success=false + Error.
/// Email ist optional - Kontakte aus dem Firmenpool haben zunaechst nur Firma und Ort
/// und bekommen die Adresse erst beim Telefonat.
/// Ein Statuswechsel wird automatisch in der Historie protokolliert.
/// Bewusst komplett im Body (kein Route-Parameter).
/// </summary>
public record SaveMarketingContactRequest(
    Guid? Id,
    string? Email,
    string? Name,
    string? Company,
    string? Phone,
    string? City,
    MarketingContactType ContactType,
    MarketingContactStatus Status,
    string? Notes
) : IRequest<SaveMarketingContactResponse>;

public record SaveMarketingContactResponse(bool Success, Guid? Id, string? Error);
