using System.Text;
using Heimatplatz.Api.Core.AiConnectorClient.Generated;
using Heimatplatz.Api.Features.Marketing.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Services;

/// <summary>
/// Generiert Marketing-E-Mail-Entwuerfe ueber den externen AiConnector-Backend-Service.
/// Der Prompt laeuft im konfigurierten Workspace (Default: projects/heimatplatz) und
/// referenziert explizit die Marketing-E-Mail-Section (sections/marketing/email/AGENTS.md),
/// die Rolle, Ton und das JSON-Ausgabeformat definiert.
/// </summary>
public class AiConnectorMarketingEmailGenerator(
    IMediator mediator,
    IOptions<MarketingOptions> options,
    ILogger<AiConnectorMarketingEmailGenerator> logger
) : IMarketingEmailGenerator
{
    public async Task<MarketingEmailDraft> GenerateAsync(string keywords, string? recipientName, CancellationToken ct = default)
    {
        var opts = options.Value.AiConnector;
        var prompt = BuildPrompt(keywords, recipientName, opts.SectionPath);

        logger.LogInformation("[Marketing] Starte AiConnector-E-Mail-Generierung im Workspace {WorkspaceId} (Section {SectionPath})",
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
                throw new TimeoutException("Die E-Mail-Generierung über den AiConnector hat das Timeout überschritten.");
            throw new InvalidOperationException(
                $"AiConnector-Lauf fehlgeschlagen (ExitCode {promptResponse.ExitCode}): {Truncate(promptResponse.Error ?? "unbekannt", 500)}");
        }

        var draft = MarketingEmailOutputParser.Parse(promptResponse.Output);

        logger.LogInformation("[Marketing] AiConnector-E-Mail-Entwurf erfolgreich in {DurationMs}ms (Betreff: {Subject})",
            promptResponse.DurationMs, draft.Subject);
        return draft;
    }

    private static string BuildPrompt(string keywords, string? recipientName, string sectionPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Lies zuerst die Datei {sectionPath}/AGENTS.md in diesem Workspace (samt der dort");
        sb.AppendLine("referenzierten uebergeordneten Marketing-Regeln) und folge deren Regeln exakt.");
        sb.AppendLine();

        sb.AppendLine("Erstelle Betreff und Text fuer eine E-Mail von Heimatplatz an einen Kontakt.");
        sb.AppendLine();

        sb.AppendLine(string.IsNullOrWhiteSpace(recipientName)
            ? "Empfaenger: (kein Name angegeben - neutrale Anrede verwenden)"
            : $"Empfaenger (fuer die Anrede): {recipientName.Trim()}");
        sb.AppendLine();

        sb.AppendLine("Stichwoerter/Inhalt (inhaltliche Basis, kann Diktat-Erkennungsfehler enthalten):");
        sb.AppendLine("\"\"\"");
        sb.AppendLine(keywords.Trim());
        sb.AppendLine("\"\"\"");
        sb.AppendLine();

        sb.AppendLine($"Antworte AUSSCHLIESSLICH mit dem JSON-Objekt gemaess {sectionPath}/AGENTS.md");
        sb.AppendLine("({\"subject\": \"...\", \"body\": \"...\"}) - keine Codezaeune, kein Text davor oder danach.");

        return sb.ToString();
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength] + "…";
}
