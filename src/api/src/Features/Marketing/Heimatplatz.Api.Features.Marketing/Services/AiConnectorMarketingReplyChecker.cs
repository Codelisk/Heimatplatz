using System.Text;
using Heimatplatz.Api.Core.AiConnectorClient.Generated;
using Heimatplatz.Api.Features.Marketing.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Services;

/// <summary>
/// Prueft Antwort-Entwuerfe ueber den externen AiConnector-Backend-Service. Der Prompt
/// laeuft im konfigurierten Workspace (Default: projects/heimatplatz) und referenziert
/// explizit die Marketing-E-Mail-Section (sections/marketing/email/AGENTS.md) fuer
/// Marke und Ton - das Ausgabeformat der Pruefung definiert aber der Prompt selbst
/// (die Section beschreibt das Generieren-Format {"subject","body"}, nicht das
/// Pruef-Format).
/// </summary>
public class AiConnectorMarketingReplyChecker(
    IMediator mediator,
    IOptions<MarketingOptions> options,
    ILogger<AiConnectorMarketingReplyChecker> logger
) : IMarketingReplyChecker
{
    public async Task<MarketingReplyCheck> CheckAsync(string conversation, string draft, CancellationToken ct = default)
    {
        var opts = options.Value.AiConnector;
        var prompt = BuildPrompt(conversation, draft, opts.SectionPath);

        logger.LogInformation("[Marketing] Starte AiConnector-Entwurfspruefung im Workspace {WorkspaceId} (Section {SectionPath})",
            opts.WorkspaceId, opts.SectionPath);

        var response = await mediator.Request(new RunPromptHttpRequest
        {
            Body = new PromptRequest
            {
                Prompt = prompt,
                WorkspaceId = opts.WorkspaceId,
                Model = opts.Model
            }
        }, ct);

        var promptResponse = response.Result;

        if (!promptResponse.Success || string.IsNullOrWhiteSpace(promptResponse.Output))
        {
            if (promptResponse.TimedOut)
                throw new TimeoutException("Die Entwurfs-Prüfung über den AiConnector hat das Timeout überschritten.");
            throw new InvalidOperationException(
                $"AiConnector-Lauf fehlgeschlagen (ExitCode {promptResponse.ExitCode}): {Truncate(promptResponse.Error ?? "unbekannt", 500)}");
        }

        var check = MarketingReplyCheckOutputParser.Parse(promptResponse.Output);

        logger.LogInformation("[Marketing] AiConnector-Entwurfspruefung erfolgreich in {DurationMs}ms (FitsContext={FitsContext}, Korrektur={HasCorrection}, Vorschlag={HasSuggestion})",
            promptResponse.DurationMs, check.FitsContext, check.CorrectedText is not null, check.SuggestedText is not null);
        return check;
    }

    private static string BuildPrompt(string conversation, string draft, string sectionPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Lies zuerst die Datei {sectionPath}/AGENTS.md in diesem Workspace (samt der dort");
        sb.AppendLine("referenzierten uebergeordneten Marketing-Regeln) - sie definiert Marke und Ton von");
        sb.AppendLine("Heimatplatz-E-Mails. Deine Aufgabe hier ist aber NICHT das Schreiben einer Mail,");
        sb.AppendLine("sondern das PRUEFEN eines Antwort-Entwurfs. Der Entwurf wird nicht versendet.");
        sb.AppendLine();

        sb.AppendLine("Bisheriger Gespraechsverlauf mit dem Kontakt (aeltester Eintrag zuerst):");
        sb.AppendLine("\"\"\"");
        sb.AppendLine(conversation.Trim());
        sb.AppendLine("\"\"\"");
        sb.AppendLine();

        sb.AppendLine("Zu pruefender Antwort-Entwurf (geht als naechste Mail von Heimatplatz an den Kontakt,");
        sb.AppendLine("die Signatur wird separat angehaengt und ist NICHT Teil der Pruefung):");
        sb.AppendLine("\"\"\"");
        sb.AppendLine(draft.Trim());
        sb.AppendLine("\"\"\"");
        sb.AppendLine();

        sb.AppendLine("Pruefe drei Dinge:");
        sb.AppendLine("1. fitsContext: Passt der Entwurf inhaltlich zum Verlauf (beantwortet er die offenen");
        sb.AppendLine("   Fragen, widerspricht er nichts Gesagtem, stimmt die Anrede zur Person)?");
        sb.AppendLine("2. correctedText: NUR Rechtschreib-, Grammatik- und Zeichensetzungsfehler minimal");
        sb.AppendLine("   korrigieren - Wortwahl, Satzbau und Inhalt unangetastet lassen. Gibt es KEINE");
        sb.AppendLine("   solchen Fehler, ist correctedText null.");
        sb.AppendLine("3. suggestedText: Nur wenn du den Entwurf spuerbar besser formulieren wuerdest");
        sb.AppendLine("   (Ton, Klarheit, fehlende Antwort auf eine Frage), schreibe deine komplette");
        sb.AppendLine("   Alternativ-Fassung (ohne Signatur/Grussformel-Namen). Ist der Entwurf gut,");
        sb.AppendLine("   ist suggestedText null - schlage nicht um des Vorschlags willen etwas vor.");
        sb.AppendLine();

        sb.AppendLine("contextNote: 1-2 kurze deutsche Saetze mit deiner Einschaetzung (bei fitsContext=false");
        sb.AppendLine("MUSS hier stehen, was fehlt oder nicht passt).");
        sb.AppendLine();

        sb.AppendLine("Antworte AUSSCHLIESSLICH mit diesem JSON-Objekt - keine Codezaeune, kein Text davor");
        sb.AppendLine("oder danach:");
        sb.AppendLine("{\"fitsContext\": true|false, \"contextNote\": \"...\", \"correctedText\": \"...\"|null, \"suggestedText\": \"...\"|null}");

        return sb.ToString();
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength] + "…";
}
