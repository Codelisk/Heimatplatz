using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;

/// <summary>
/// Uebernimmt ausgewaehlte Firmen aus dem Firmenpool als Kontakte mit Status
/// "Zu kontaktieren". Je Firma wird der volle Datensatz live beim Firmenpool geholt
/// (daher das niedrige Limit) - Adresse und Geschaeftsfuehrung landen in der Startnotiz.
/// Idempotent: bereits uebernommene Firmen (gleiche Firmenbuchnummer) werden
/// uebersprungen und in Skipped gezaehlt, nicht als Fehler gemeldet.
/// </summary>
public record AddMarketingLeadsRequest(
    List<string> Fnrs
) : IRequest<AddMarketingLeadsResponse>;

public record AddMarketingLeadsResponse(
    bool Success,
    int Added,
    int Skipped,
    string? Error
);
