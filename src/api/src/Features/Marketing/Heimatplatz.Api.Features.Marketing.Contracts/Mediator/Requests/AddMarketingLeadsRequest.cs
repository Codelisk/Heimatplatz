using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;

/// <summary>
/// Uebernimmt ausgewaehlte Firmenbuch-Firmen als Kontakte mit Status "Zu kontaktieren".
/// Idempotent: bereits uebernommene Firmen (gleiche Firmenbuchnummer) werden
/// uebersprungen und in Skipped gezaehlt, nicht als Fehler gemeldet.
/// </summary>
public record AddMarketingLeadsRequest(
    List<Guid> FirmenbuchCompanyIds
) : IRequest<AddMarketingLeadsResponse>;

public record AddMarketingLeadsResponse(
    bool Success,
    int Added,
    int Skipped,
    string? Error
);
