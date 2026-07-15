using System.Text.Json;
using System.Text.Json.Serialization;
using Heimatplatz.Api.Features.AiListing.Contracts.Models;

namespace Heimatplatz.Api.Features.AiListing.Services;

/// <summary>
/// Extrahiert das ExtractedListingData-JSON aus einer rohen KI-Antwort.
/// KIs geben teils zusaetzlichen Text vor/nach dem JSON aus, daher wird das
/// erste vollstaendige JSON-Objekt aus der Ausgabe geschnitten.
/// </summary>
internal static class ListingResultParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static ExtractedListingData Parse(string rawOutput)
    {
        var start = rawOutput.IndexOf('{');
        var end = rawOutput.LastIndexOf('}');
        if (start < 0 || end <= start)
            throw new InvalidOperationException(
                $"KI-Antwort enthaelt kein JSON-Objekt: {Truncate(rawOutput, 500)}");

        var json = rawOutput.Substring(start, end - start + 1);

        var result = JsonSerializer.Deserialize<ExtractedListingData>(json, JsonOptions)
            ?? throw new InvalidOperationException("KI-Antwort konnte nicht deserialisiert werden.");

        if (string.IsNullOrWhiteSpace(result.Title) || string.IsNullOrWhiteSpace(result.Description))
            throw new InvalidOperationException("KI-Antwort enthaelt keinen Titel oder keine Beschreibung.");

        return result;
    }

    public static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength] + "…";
}
