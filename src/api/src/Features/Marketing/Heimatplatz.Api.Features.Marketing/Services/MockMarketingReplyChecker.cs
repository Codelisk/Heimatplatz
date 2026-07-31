using Microsoft.Extensions.Logging;

namespace Heimatplatz.Api.Features.Marketing.Services;

/// <summary>
/// Dev-Provider ohne echte KI: liefert ein erkennbares Platzhalter-Ergebnis mit
/// Korrektur UND Vorschlag, damit das komplette Panel-UI (Kontext-Urteil, Korrektur
/// uebernehmen, Vorschlag uebernehmen, Rueckgaengig) lokal ohne AiConnector-Zugang
/// testbar ist. Anders als beim E-Mail-Generator ist kein Versand-Blocker noetig -
/// die Pruefung veraendert nichts, uebernommener Text ist bewusste Nutzer-Entscheidung.
/// </summary>
public class MockMarketingReplyChecker(ILogger<MockMarketingReplyChecker> logger) : IMarketingReplyChecker
{
    public Task<MarketingReplyCheck> CheckAsync(
        string conversation,
        string draft,
        string? instruction = null,
        string? previousSuggestion = null,
        CancellationToken ct = default)
    {
        logger.LogInformation("[Marketing] Mock-Entwurfspruefung ({DraftLength} Zeichen Entwurf, {ConversationLength} Zeichen Verlauf, Anweisung: {HasInstruction})",
            draft.Length, conversation.Length, !string.IsNullOrWhiteSpace(instruction));

        // Ueberarbeitungs-Runde: Anweisung sichtbar in den Vorschlag einbauen, damit
        // der Refine-Flow im UI nachvollziehbar ist
        var suggested = string.IsNullOrWhiteSpace(instruction)
            ? $"{draft.Trim()}\n\n(Mock-Formulierungsvorschlag ohne KI)"
            : $"{(string.IsNullOrWhiteSpace(previousSuggestion) ? draft : previousSuggestion).Trim()}\n\n(Mock-Überarbeitung ohne KI: {instruction.Trim()})";

        return Task.FromResult(new MarketingReplyCheck(
            FitsContext: true,
            ContextNote: "Mock-Prüfung ohne KI – am Server prüft der konfigurierte Provider (Marketing__Provider=AiConnector).",
            CorrectedText: $"{draft.Trim()}\n\n(Mock-Korrektur ohne KI)",
            SuggestedText: suggested
        ));
    }
}
