namespace Heimatplatz.Api.Features.Marketing.Services;

/// <summary>Fertiger E-Mail-Entwurf des Generators (ohne Signatur).</summary>
public record MarketingEmailDraft(string Subject, string Body);

/// <summary>
/// Erstellt einen Marketing-E-Mail-Entwurf (Betreff + Text) aus Stichwoertern.
/// Implementierungen: MockMarketingEmailGenerator (Dev) und
/// AiConnectorMarketingEmailGenerator (echte KI ueber den AiConnector-Workspace).
/// </summary>
public interface IMarketingEmailGenerator
{
    /// <summary>
    /// Wirft TimeoutException/InvalidOperationException bei fehlgeschlagener
    /// Generierung - der Handler uebersetzt das in eine Fehler-Response.
    /// </summary>
    Task<MarketingEmailDraft> GenerateAsync(string keywords, string? recipientName, CancellationToken ct = default);
}
