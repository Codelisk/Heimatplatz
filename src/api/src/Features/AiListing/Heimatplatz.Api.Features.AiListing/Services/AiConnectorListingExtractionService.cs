using System.Text;
using Heimatplatz.Api.Core.AiConnectorClient.Generated;
using Heimatplatz.Api.Features.AiListing.Configuration;
using Heimatplatz.Api.Features.AiListing.Contracts.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.AiListing.Services;

/// <summary>
/// Extrahiert Inseratsdaten ueber den externen AiConnector-Backend-Service.
/// Der Prompt wird im konfigurierten Workspace (Default: projects/heimatplatz)
/// ausgefuehrt - dessen AGENTS.md/CLAUDE.md definieren die Experten-Rolle und
/// das erwartete JSON-Ausgabeformat. Der Aufruf laeuft ueber den aus
/// AiConnector.json generierten Shiny.Mediator-HTTP-Client
/// (Heimatplatz.Api.Core.AiConnectorClient) statt ueber einen manuellen HttpClient.
/// </summary>
public class AiConnectorListingExtractionService(
    IMediator mediator,
    IOptions<AiListingOptions> options,
    ILogger<AiConnectorListingExtractionService> logger
) : IListingExtractionService
{
    public async Task<ExtractedListingData> ExtractAsync(ListingExtractionInput input, CancellationToken ct = default)
    {
        var opts = options.Value.AiConnector;

        if (string.IsNullOrWhiteSpace(input.DictatedText) && string.IsNullOrWhiteSpace(input.UserNotes))
            throw new InvalidOperationException(
                "Der AiConnector-Provider benoetigt eine diktierte Beschreibung oder Notizen (Medien werden nicht uebertragen).");

        var prompt = BuildPrompt(input);
        logger.LogInformation("[AiListing] Starte AiConnector-Extraktion im Workspace {WorkspaceId}", opts.WorkspaceId);

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
                throw new TimeoutException("KI-Analyse ueber AiConnector hat das Timeout ueberschritten.");
            throw new InvalidOperationException(
                $"AiConnector-Lauf fehlgeschlagen (ExitCode {promptResponse.ExitCode}): {ListingResultParser.Truncate(promptResponse.Error ?? "unbekannt", 500)}");
        }

        var result = ListingResultParser.Parse(promptResponse.Output);
        logger.LogInformation("[AiListing] AiConnector-Extraktion erfolgreich in {DurationMs}ms: {Title}",
            promptResponse.DurationMs, result.Title);
        return result;
    }

    private static string BuildPrompt(ListingExtractionInput input)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Erstelle aus den folgenden Verkaeufer-Angaben ein KI-Inserat gemaess den Regeln");
        sb.AppendLine("dieses Workspaces (AGENTS.md). Antworte AUSSCHLIESSLICH mit dem einzelnen");
        sb.AppendLine("JSON-Objekt im dort definierten Schema.");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(input.DictatedText))
        {
            sb.AppendLine("Diktierte Beschreibung des Verkaeufers (Sprache-zu-Text, kann Erkennungsfehler enthalten):");
            sb.AppendLine("\"\"\"");
            sb.AppendLine(input.DictatedText);
            sb.AppendLine("\"\"\"");
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(input.UserNotes))
        {
            sb.AppendLine("Zusaetzliche Notizen des Verkaeufers:");
            sb.AppendLine("\"\"\"");
            sb.AppendLine(input.UserNotes);
            sb.AppendLine("\"\"\"");
            sb.AppendLine();
        }

        if (input.ImagePaths.Count > 0 || input.VideoPaths.Count > 0)
        {
            sb.AppendLine($"Hinweis: Der Verkaeufer hat zusaetzlich {input.ImagePaths.Count} Foto(s) und " +
                          $"{input.VideoPaths.Count} Video(s) hochgeladen, die hier nicht uebertragen werden. " +
                          "Erstelle das Inserat ausschliesslich aus den Textangaben.");
        }

        return sb.ToString();
    }
}
