using Heimatplatz.Api.Features.Marketing.Contracts.Models;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;

/// <summary>
/// Posteingang (Rueckmeldungen, neueste zuerst). Loest vor dem Lesen einen
/// gedrosselten IMAP-Sync aus (max. alle 5 Minuten), sofern das Postfach
/// konfiguriert ist - die Seite ist damit beim Oeffnen aktuell.
/// </summary>
public record GetMarketingInboxRequest(
    bool UnreadOnly = false,
    int Page = 0,
    int PageSize = 50
) : IRequest<GetMarketingInboxResponse>;

/// <summary>
/// ImapConfigured=false: Postfach-Abruf nicht moeglich (EMAIL_SMTP_* fehlt), die Liste
/// zeigt dann nur bereits gespeicherte Eintraege. SyncError = Fehler des Auto-Syncs
/// (Anzeige als Banner), null wenn erfolgreich oder uebersprungen.
/// </summary>
public record GetMarketingInboxResponse(
    List<MarketingInboundEmailDto> Items,
    int Total,
    int PageSize,
    int CurrentPage,
    bool HasMore,
    bool ImapConfigured,
    string? SyncError
);
