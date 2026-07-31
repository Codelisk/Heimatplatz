using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;

/// <summary>
/// Zusatzadresse zu einem Kontakt hinzufuegen (z.B. persoenliche Adresse eines
/// Ansprechpartners neben der office@-Versand-Adresse). Die Adresse wird normalisiert
/// und darf keinem anderen Kontakt gehoeren (weder als Versand- noch als Zusatzadresse).
/// </summary>
public record AddMarketingContactEmailRequest(
    Guid ContactId,
    string Email
) : IRequest<MarketingContactEmailActionResponse>;

/// <summary>
/// Zusatzadresse eines Kontakts entfernen (identifiziert ueber die Adresse selbst -
/// Zusatzadressen sind global eindeutig).
/// </summary>
public record RemoveMarketingContactEmailRequest(
    Guid ContactId,
    string Email
) : IRequest<MarketingContactEmailActionResponse>;

public record MarketingContactEmailActionResponse(bool Success, string? Error);
