using Heimatplatz.Api.Features.Legal.Contracts.Models;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Legal.Contracts.Mediator.Requests;

/// <summary>
/// Aendert die Zusatz-Kontaktdaten (SettingType "Contact") - Admin-only.
///
/// Vollstaendiges Ersetzen, kein Merge: das Intern-Formular schickt alle Felder
/// vorbefuellt zurueck, damit "Feld leeren" eindeutig von "Feld nicht mitgeschickt"
/// unterscheidbar bleibt. Leere Strings werden als "nicht gepflegt" (null) gespeichert.
/// </summary>
public record UpdateContactSettingsRequest(
    string? Email,
    string? SupportEmail,
    string? Phone,
    string? Website,
    string? OfficeHours,
    List<SocialLinkDto>? SocialLinks
) : IRequest<UpdateContactSettingsResponse>;

public record UpdateContactSettingsResponse(
    bool Success,
    string? Error,
    LegalContactInfoDto? Contact
);
