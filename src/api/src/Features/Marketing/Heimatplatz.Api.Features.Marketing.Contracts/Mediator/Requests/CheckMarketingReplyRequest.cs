using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;

/// <summary>
/// KI-Pruefung eines Antwort-Entwurfs aus dem Kontakt-Chat-Verlauf, BEVOR er versendet
/// wird: Passt der Entwurf zum bisherigen Gespraechsverlauf? Stimmen Rechtschreibung
/// und Grammatik? Wuerde die KI etwas anders formulieren? Der Entwurf wird dabei NICHT
/// versendet und nichts gespeichert - reine Beratung. Die Eingangs-Mail liefert den
/// Kontakt und damit den Verlauf als Pruefkontext.
/// </summary>
public record CheckMarketingReplyRequest(
    Guid InboundEmailId,
    string Draft
) : IRequest<CheckMarketingReplyResponse>;

/// <summary>
/// Pruefergebnis. CorrectedText/SuggestedText sind null, wenn es nichts zu
/// korrigieren bzw. keinen besseren Formulierungsvorschlag gibt - die Abwesenheit
/// ist die Aussage "passt so".
/// </summary>
/// <param name="FitsContext">Passt der Entwurf inhaltlich zum Gespraechsverlauf?</param>
/// <param name="ContextNote">Kurze Einschaetzung (1-2 Saetze) zur Kontext-Passung</param>
/// <param name="CorrectedText">Rechtschreib-/Grammatik-korrigierte Fassung; null = keine Fehler</param>
/// <param name="SuggestedText">Alternative Formulierung der KI; null = Entwurf passt so</param>
public record CheckMarketingReplyResponse(
    bool Success,
    bool FitsContext,
    string? ContextNote,
    string? CorrectedText,
    string? SuggestedText,
    string? Error
);
