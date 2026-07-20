using System.Text;
using Heimatplatz.Api.Core.AiConnectorClient.Generated;
using Heimatplatz.Api.Features.AiListing.Configuration;
using Heimatplatz.Api.Features.AiListing.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Properties.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.AiListing.Services;

/// <summary>
/// Generiert die Inserats-Beschreibung ueber den externen AiConnector-Backend-Service.
/// Der Prompt wird im konfigurierten Workspace (Default: projects/heimatplatz) ausgefuehrt.
/// Fotos werden als oeffentliche URLs mitgegeben - der Workspace-Agent kann sie abrufen
/// und sichtbare Details einfliessen lassen; die Bilddaten selbst werden nicht uebertragen.
/// </summary>
public class AiConnectorListingDescriptionService(
    IMediator mediator,
    IOptions<AiListingOptions> options,
    ILogger<AiConnectorListingDescriptionService> logger
) : IListingDescriptionService
{
    public async Task<string> GenerateAsync(GenerateListingDescriptionRequest input, CancellationToken ct = default)
    {
        var opts = options.Value;
        var prompt = BuildPrompt(input, opts.Description);

        logger.LogInformation("[AiListing] Starte AiConnector-Beschreibung im Workspace {WorkspaceId}",
            opts.AiConnector.WorkspaceId);

        var response = await mediator.Request(new RunPromptHttpRequest
        {
            Body = new PromptRequest
            {
                Prompt = prompt,
                WorkspaceId = opts.AiConnector.WorkspaceId,
                Model = opts.AiConnector.Model
            }
        }, ct);

        var promptResponse = response.Result;

        if (!promptResponse.Success || string.IsNullOrWhiteSpace(promptResponse.Output))
        {
            if (promptResponse.TimedOut)
                throw new TimeoutException("Beschreibungs-Generierung ueber AiConnector hat das Timeout ueberschritten.");
            throw new InvalidOperationException(
                $"AiConnector-Lauf fehlgeschlagen (ExitCode {promptResponse.ExitCode}): {Truncate(promptResponse.Error ?? "unbekannt", 500)}");
        }

        var description = CleanOutput(promptResponse.Output);
        if (string.IsNullOrWhiteSpace(description))
            throw new InvalidOperationException("AiConnector hat keinen Beschreibungstext geliefert.");

        var wordCount = description.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        if (wordCount < opts.Description.MinWords / 2 || wordCount > opts.Description.MaxWords * 2)
            logger.LogWarning("[AiListing] Beschreibung deutlich ausserhalb des Wortbereichs ({WordCount} statt {Min}-{Max})",
                wordCount, opts.Description.MinWords, opts.Description.MaxWords);

        logger.LogInformation("[AiListing] AiConnector-Beschreibung erfolgreich in {DurationMs}ms ({WordCount} Woerter)",
            promptResponse.DurationMs, wordCount);
        return description;
    }

    private static string BuildPrompt(GenerateListingDescriptionRequest input, ListingDescriptionOptions descriptionOptions)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Lies zuerst die Datei sections/inserate/AGENTS.md in diesem Workspace und schreibe");
        sb.AppendLine("die Beschreibung fuer ein Immobilien-Inserat gemaess deren Regeln. Gefragt ist");
        sb.AppendLine("AUSSCHLIESSLICH der fertige Beschreibungstext als Fliesstext.");
        sb.AppendLine();

        sb.AppendLine("Eckdaten (verbindlich - NICHTS hinzuerfinden):");
        if (input.Type is { } type)
            sb.AppendLine($"- Objektart: {(type == PropertyType.Land ? "Grundstueck" : "Haus")}");
        if (!string.IsNullOrWhiteSpace(input.Title))
            sb.AppendLine($"- Titel des Inserats: {input.Title}");
        if (input.Rooms is { } rooms)
            sb.AppendLine($"- Zimmer: {rooms}");
        if (input.LivingAreaSquareMeters is { } living)
            sb.AppendLine($"- Wohnflaeche: ca. {living} m²");
        if (input.PlotAreaSquareMeters is { } plot)
            sb.AppendLine($"- Grundflaeche: ca. {plot} m²");
        if (input.YearBuilt is { } year)
            sb.AppendLine($"- Baujahr: {year}");
        if (input.Features is { Count: > 0 })
            sb.AppendLine($"- Ausstattung: {string.Join(", ", input.Features)}");
        if (!string.IsNullOrWhiteSpace(input.MunicipalityDisplay))
            sb.AppendLine($"- Ort: {input.MunicipalityDisplay}");
        sb.AppendLine();

        sb.AppendLine("Stichwoerter/Notizen des Verkaeufers (inhaltliche Basis, kann Diktat-Erkennungsfehler enthalten):");
        sb.AppendLine("\"\"\"");
        sb.AppendLine(input.Keywords);
        sb.AppendLine("\"\"\"");
        sb.AppendLine();

        if (input.ImageUrls is { Count: > 0 })
        {
            sb.AppendLine($"Der Verkaeufer hat {input.ImageUrls.Count} Foto(s) hochgeladen. Falls du URLs abrufen");
            sb.AppendLine("kannst, sieh dir die Fotos an und lass konkrete, tatsaechlich sichtbare Details in den");
            sb.AppendLine("Text einfliessen (keine Spekulation). Falls nicht, ignoriere die Links kommentarlos:");
            foreach (var url in input.ImageUrls.Take(10))
                sb.AppendLine($"- {url}");
            sb.AppendLine();
        }

        sb.AppendLine("Regeln:");
        sb.AppendLine($"- Deutsch, {descriptionOptions.MinWords} bis {descriptionOptions.MaxWords} Woerter, 2-4 Absaetze Fliesstext ohne Ueberschriften und ohne Aufzaehlungen.");
        sb.AppendLine("- Sachlich-einladender Ton; keine Superlative, keine Uebertreibungen, keine unbelegten Behauptungen.");
        sb.AppendLine("- Keinen Preis, keine Strassenadresse und keine Kontaktdaten nennen.");
        sb.AppendLine("- Antworte AUSSCHLIESSLICH mit dem fertigen Beschreibungstext: kein JSON, kein Markdown, keine Anfuehrungszeichen, keine Vor- oder Nachbemerkungen.");

        return sb.ToString();
    }

    /// <summary>
    /// Entfernt typische Verpackungen der KI-Ausgabe (Markdown-Zaeune, umschliessende
    /// Anfuehrungszeichen), ohne den eigentlichen Text anzutasten.
    /// </summary>
    private static string CleanOutput(string rawOutput)
    {
        var text = rawOutput.Trim();

        if (text.StartsWith("```", StringComparison.Ordinal))
        {
            var firstLineEnd = text.IndexOf('\n');
            var lastFence = text.LastIndexOf("```", StringComparison.Ordinal);
            if (firstLineEnd >= 0 && lastFence > firstLineEnd)
                text = text[(firstLineEnd + 1)..lastFence].Trim();
        }

        if (text.Length >= 2 && text[0] == '"' && text[^1] == '"')
            text = text[1..^1].Trim();

        return text;
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength] + "…";
}
