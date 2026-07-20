namespace Heimatplatz.Api.Features.Marketing.Services;

/// <summary>
/// Ergebnis eines Posteingang-Abrufs. Skipped=true: Sync wurde nicht ausgefuehrt
/// (Drossel aktiv, bereits ein Sync im Gange oder IMAP nicht konfiguriert).
/// </summary>
public record MarketingInboxSyncResult(bool Success, bool Skipped, int Added, string? Error);

/// <summary>
/// Ruft das Postfach info@heimatplatz.at per IMAP ab und uebernimmt marketing-relevante
/// Mails (Antworten auf versendete Marketing-Mails bzw. Mails bekannter Kontakte) in
/// die MarketingInboundEmails-Tabelle.
/// </summary>
public interface IMarketingInboxSync
{
    /// <summary>Postfach-Abruf moeglich (Email:SmtpHost + Zugangsdaten konfiguriert)</summary>
    bool IsConfigured { get; }

    /// <summary>
    /// force=false: gedrosselt (max. alle 5 Minuten, fuer den Auto-Sync beim Oeffnen
    /// der Posteingang-Seite); force=true: sofort ("Jetzt abrufen").
    /// </summary>
    Task<MarketingInboxSyncResult> SyncAsync(bool force, CancellationToken ct = default);
}
