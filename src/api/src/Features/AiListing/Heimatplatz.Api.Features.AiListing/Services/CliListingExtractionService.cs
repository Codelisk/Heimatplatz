using System.Diagnostics;
using System.Text;
using Heimatplatz.Api.Features.AiListing.Configuration;
using Heimatplatz.Api.Features.AiListing.Contracts.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Heimatplatz.Api.Features.AiListing.Services;

/// <summary>
/// Extrahiert Inseratsdaten ueber eine am Server installierte Agent-CLI
/// (z.B. claude oder codex). Der Prompt wird ueber stdin uebergeben, die CLI
/// kann die Mediendateien ueber ihre Datei-Tools lesen und muss als Antwort
/// ein einzelnes JSON-Objekt liefern.
/// </summary>
public class CliListingExtractionService(
    IOptions<AiListingOptions> options,
    ILogger<CliListingExtractionService> logger
) : IListingExtractionService
{
    public async Task<ExtractedListingData> ExtractAsync(ListingExtractionInput input, CancellationToken ct = default)
    {
        var opts = options.Value;
        var prompt = BuildPrompt(input);

        var startInfo = new ProcessStartInfo
        {
            FileName = opts.CliCommand,
            Arguments = opts.CliArguments,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        if (!string.IsNullOrWhiteSpace(opts.WorkingDirectory))
            startInfo.WorkingDirectory = opts.WorkingDirectory;

        logger.LogInformation("[AiListing] Starte CLI-Extraktion: {Command} {Arguments}", opts.CliCommand, opts.CliArguments);

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
            throw new InvalidOperationException($"CLI-Prozess '{opts.CliCommand}' konnte nicht gestartet werden.");

        await process.StandardInput.WriteAsync(prompt);
        process.StandardInput.Close();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = process.StandardError.ReadToEndAsync(ct);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(opts.TimeoutSeconds));

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* Prozess bereits beendet */ }
            throw new TimeoutException($"KI-Analyse hat das Timeout von {opts.TimeoutSeconds}s ueberschritten.");
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (process.ExitCode != 0)
        {
            logger.LogError("[AiListing] CLI-Prozess fehlgeschlagen (ExitCode {ExitCode}): {Stderr}",
                process.ExitCode, Truncate(stderr, 2000));
            throw new InvalidOperationException(
                $"KI-CLI beendete mit ExitCode {process.ExitCode}: {Truncate(stderr, 500)}");
        }

        var result = ParseResult(stdout);
        logger.LogInformation("[AiListing] CLI-Extraktion erfolgreich: {Title}", result.Title);
        return result;
    }

    private static ExtractedListingData ParseResult(string stdout) =>
        ListingResultParser.Parse(stdout);

    private static string BuildPrompt(ListingExtractionInput input)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Du bist ein Immobilien-Experte und erstellst aus Fotos, Videos und einer diktierten");
        sb.AppendLine("Beschreibung ein professionelles Immobilien-Inserat auf Deutsch (oesterreichischer Markt).");
        sb.AppendLine();

        if (input.ImagePaths.Count > 0)
        {
            sb.AppendLine("Analysiere diese Fotos der Immobilie (lies die Dateien mit deinen Tools):");
            foreach (var path in input.ImagePaths)
                sb.AppendLine($"- {path}");
            sb.AppendLine();
        }

        if (input.VideoPaths.Count > 0)
        {
            sb.AppendLine("Zusaetzlich existieren diese Videos (falls du Videos nicht lesen kannst, ignoriere sie):");
            foreach (var path in input.VideoPaths)
                sb.AppendLine($"- {path}");
            sb.AppendLine();
        }

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

        sb.AppendLine("Erstelle daraus die Inseratsdaten. Antworte AUSSCHLIESSLICH mit einem einzelnen JSON-Objekt");
        sb.AppendLine("in exakt diesem Schema (keine Erklaerungen, kein Markdown):");
        sb.AppendLine("""
{
  "Title": "Ansprechender Inserats-Titel (max. 100 Zeichen)",
  "Description": "Professionelle, ansprechende Beschreibung (300-1500 Zeichen)",
  "Type": "House oder Land",
  "Rooms": 4,
  "LivingAreaSquareMeters": 120,
  "PlotAreaSquareMeters": 600,
  "YearBuilt": 1995,
  "Features": ["Garage", "Garten", "Keller"],
  "Summary": "1-2 Saetze, was du aus den Medien erkannt hast"
}
""");
        sb.AppendLine();
        sb.AppendLine("Regeln:");
        sb.AppendLine("- Nenne KEINEN Preis, KEINE Adresse und KEINE Kontaktdaten (werden manuell erfasst).");
        sb.AppendLine("- Unbekannte Zahlenwerte als null. Erfinde keine Werte, die weder sichtbar noch diktiert sind.");
        sb.AppendLine("- \"Type\": \"Land\" nur bei unbebauten Grundstuecken, sonst \"House\".");
        sb.AppendLine("- Features: kurze deutsche Begriffe (z.B. Garage, Garten, Balkon, Photovoltaik).");

        return sb.ToString();
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength] + "…";
}
