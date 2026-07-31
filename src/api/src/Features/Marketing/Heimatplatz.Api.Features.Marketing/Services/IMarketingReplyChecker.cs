namespace Heimatplatz.Api.Features.Marketing.Services;

/// <summary>
/// Ergebnis der Entwurfs-Pruefung. CorrectedText/SuggestedText null = nichts zu
/// korrigieren bzw. kein besserer Vorschlag.
/// </summary>
public record MarketingReplyCheck(
    bool FitsContext,
    string ContextNote,
    string? CorrectedText,
    string? SuggestedText
);

/// <summary>
/// Prueft einen Antwort-Entwurf gegen den Gespraechsverlauf eines Kontakts:
/// Kontext-Passung, Rechtschreibung/Grammatik und Formulierung. Implementierungen:
/// MockMarketingReplyChecker (Dev) und AiConnectorMarketingReplyChecker (echte KI
/// ueber den AiConnector-Workspace).
/// </summary>
public interface IMarketingReplyChecker
{
    /// <summary>
    /// <paramref name="conversation"/> ist der vorbereitete Klartext-Verlauf
    /// (Handler baut ihn aus Versand/Rueckmeldungen). Wirft
    /// TimeoutException/InvalidOperationException bei fehlgeschlagener Pruefung -
    /// der Handler uebersetzt das in eine Fehler-Response.
    /// </summary>
    Task<MarketingReplyCheck> CheckAsync(string conversation, string draft, CancellationToken ct = default);
}
