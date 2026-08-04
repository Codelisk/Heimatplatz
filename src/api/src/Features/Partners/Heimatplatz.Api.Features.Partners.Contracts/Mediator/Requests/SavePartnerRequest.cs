using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Partners.Contracts.Mediator.Requests;

/// <summary>
/// Legt einen Partner an (Id=null) oder ersetzt einen bestehenden komplett (Id gesetzt).
/// Vollstaendiges Ersetzen wie UpdateContactSettingsRequest: das Intern-Formular schickt
/// alle Felder vorbefuellt zurueck, leere Strings werden als null gespeichert. Admin-only.
/// </summary>
public record SavePartnerRequest(
    Guid? Id,
    string Name,
    string Category,
    string? Description,
    string? WebsiteUrl,
    string? LogoUrl,
    string? Region,
    int? PartnerSinceYear,
    string? SourceName,
    string? SellerName,
    int DisplayOrder,
    bool IsVisible
) : IRequest<SavePartnerResponse>;

public record SavePartnerResponse(
    bool Success,
    string? Error,
    Guid? Id
);
