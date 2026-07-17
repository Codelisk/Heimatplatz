using System.Text;
using Heimatplatz.Api.Features.AiListing.Configuration;
using Heimatplatz.Api.Features.AiListing.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Properties.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Heimatplatz.Api.Features.AiListing.Services;

/// <summary>
/// Dev-Provider ohne echte KI: baut eine plausible Beschreibung aus den Eckdaten und den
/// Stichwoertern. Die konfigurierbare Verzoegerung macht den asynchronen Job-Flow
/// (Queued -> InProgress -> Finished) in der App sichtbar und testbar.
/// </summary>
public class MockListingDescriptionService(
    IOptions<AiListingOptions> options,
    ILogger<MockListingDescriptionService> logger
) : IListingDescriptionService
{
    public async Task<string> GenerateAsync(GenerateListingDescriptionRequest input, CancellationToken ct = default)
    {
        var opts = options.Value.Description;

        await Task.Delay(TimeSpan.FromSeconds(opts.MockDelaySeconds), ct);

        var isLand = input.Type == PropertyType.Land;
        var objectWord = isLand ? "Grundstueck" : "Haus";

        var facts = new List<string>();
        if (input.Rooms is { } rooms) facts.Add($"{rooms} Zimmer");
        if (input.LivingAreaSquareMeters is { } living) facts.Add($"rund {living} m² Wohnflaeche");
        if (input.PlotAreaSquareMeters is { } plot) facts.Add($"etwa {plot} m² Grundflaeche");
        if (input.YearBuilt is { } year) facts.Add($"Baujahr {year}");

        var sb = new StringBuilder();

        sb.Append(string.IsNullOrWhiteSpace(input.MunicipalityDisplay)
            ? $"Dieses {objectWord} befindet sich in einer gefragten Lage."
            : $"Dieses {objectWord} befindet sich in {input.MunicipalityDisplay}.");
        if (facts.Count > 0)
            sb.Append($" Es bietet {string.Join(", ", facts)}.");

        if (input.Features is { Count: > 0 })
            sb.Append($" Zur Ausstattung zaehlen {string.Join(", ", input.Features)}.");

        sb.AppendLine();
        sb.AppendLine();
        sb.Append("Die Angaben des Verkaeufers beschreiben das Objekt so: ");
        sb.Append(input.Keywords.Trim());
        if (!input.Keywords.TrimEnd().EndsWith('.'))
            sb.Append('.');

        sb.AppendLine();
        sb.AppendLine();
        sb.Append("Die Umgebung punktet mit guter Erreichbarkeit und Infrastruktur des taeglichen Bedarfs. ");
        sb.Append(isLand
            ? "Das Grundstueck eignet sich fuer die Verwirklichung eines individuellen Bauvorhabens - eine Besichtigung vermittelt den besten Eindruck von Zuschnitt und Lage."
            : "Bei einer Besichtigung lassen sich Raumaufteilung, Zustand und Umgebung am besten beurteilen - vereinbaren Sie dazu einfach einen Termin ueber die Plattform.");

        sb.AppendLine();
        sb.AppendLine();
        sb.Append("(Mock-Beschreibung ohne KI - am Server erstellt der konfigurierte Provider den finalen Text ");
        sb.Append($"im Bereich {opts.MinWords}-{opts.MaxWords} Woerter.)");

        var description = sb.ToString();
        logger.LogInformation("[AiListing] Mock-Beschreibung erstellt ({Length} Zeichen, {Images} Foto-URLs ignoriert)",
            description.Length, input.ImageUrls?.Count ?? 0);
        return description;
    }
}
