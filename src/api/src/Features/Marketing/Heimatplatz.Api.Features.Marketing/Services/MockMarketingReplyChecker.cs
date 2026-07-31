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
    public Task<MarketingReplyCheck> CheckAsync(string conversation, string draft, CancellationToken ct = default)
    {
        logger.LogInformation("[Marketing] Mock-Entwurfspruefung ({DraftLength} Zeichen Entwurf, {ConversationLength} Zeichen Verlauf)",
            draft.Length, conversation.Length);

        return Task.FromResult(new MarketingReplyCheck(
            FitsContext: true,
            ContextNote: "Mock-Prüfung ohne KI – am Server prüft der konfigurierte Provider (Marketing__Provider=AiConnector).",
            CorrectedText: $"{draft.Trim()}\n\n(Mock-Korrektur ohne KI)",
            SuggestedText: $"{draft.Trim()}\n\n(Mock-Formulierungsvorschlag ohne KI)"
        ));
    }
}
