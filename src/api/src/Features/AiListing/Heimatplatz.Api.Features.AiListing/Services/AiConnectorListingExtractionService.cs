using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Heimatplatz.Api.Features.AiListing.Configuration;
using Heimatplatz.Api.Features.AiListing.Contracts.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Heimatplatz.Api.Features.AiListing.Services;

/// <summary>
/// Extrahiert Inseratsdaten ueber den externen AiConnector-Backend-Service.
/// Der Prompt wird im konfigurierten Workspace (Default: projects/heimatplatz)
/// ausgefuehrt - dessen AGENTS.md/CLAUDE.md definieren die Experten-Rolle und
/// das erwartete JSON-Ausgabeformat. Der Service uebertraegt daher nur die
/// Textangaben des Verkaeufers und parst die JSON-Antwort.
/// </summary>
public class AiConnectorListingExtractionService(
    HttpClient httpClient,
    IOptions<AiListingOptions> options,
    ILogger<AiConnectorListingExtractionService> logger
) : IListingExtractionService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ExtractedListingData> ExtractAsync(ListingExtractionInput input, CancellationToken ct = default)
    {
        var opts = options.Value.AiConnector;

        if (string.IsNullOrWhiteSpace(input.DictatedText) && string.IsNullOrWhiteSpace(input.UserNotes))
            throw new InvalidOperationException(
                "Der AiConnector-Provider benoetigt eine diktierte Beschreibung oder Notizen (Medien werden nicht uebertragen).");

        var prompt = BuildPrompt(input);
        logger.LogInformation("[AiListing] Starte AiConnector-Extraktion im Workspace {WorkspaceId}", opts.WorkspaceId);

        using var response = await httpClient.PostAsJsonAsync("/api/prompt", new
        {
            prompt,
            workspaceId = opts.WorkspaceId,
            model = opts.Model
        }, JsonOptions, ct);

        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("[AiListing] AiConnector antwortete mit {StatusCode}: {Body}",
                (int)response.StatusCode, ListingResultParser.Truncate(body, 2000));
            throw new InvalidOperationException(
                $"AiConnector antwortete mit HTTP {(int)response.StatusCode}: {ListingResultParser.Truncate(body, 500)}");
        }

        var promptResponse = JsonSerializer.Deserialize<AiConnectorPromptResponse>(body, JsonOptions)
            ?? throw new InvalidOperationException("AiConnector-Antwort konnte nicht gelesen werden.");

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

    /// <summary>Antwort-Shape von POST /api/prompt des AiConnectors</summary>
    private sealed record AiConnectorPromptResponse(
        bool Success,
        int? ExitCode,
        string? Output,
        string? Error,
        long? DurationMs,
        bool TimedOut,
        bool Canceled,
        string? RequestId);
}
