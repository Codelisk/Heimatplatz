using Heimatplatz.Api.Features.Marketing.Contracts.Models;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;

/// <summary>
/// Kontakt anlegen (Id=null) oder bearbeiten (Id gesetzt). Die E-Mail-Adresse wird
/// serverseitig normalisiert (lowercase/trim); Dubletten liefern Success=false + Error.
/// Email ist optional - Kontakte aus dem Firmenpool haben zunaechst nur Firma und Ort
/// und bekommen die Adresse erst beim Telefonat.
/// Die Ansprechperson kommt strukturiert (Salutation/Title/FirstName/LastName) - daraus
/// setzt der Handler den Anzeigenamen zusammen. <see cref="Name"/> bleibt als
/// Alt-Bestand-Fallback erhalten und greift nur, wenn alle Namensteile leer sind.
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
    string? Notes,
    MarketingSalutation Salutation = MarketingSalutation.Unknown,
    string? Title = null,
    string? FirstName = null,
    string? LastName = null
) : IRequest<SaveMarketingContactResponse>;

public record SaveMarketingContactResponse(bool Success, Guid? Id, string? Error);
