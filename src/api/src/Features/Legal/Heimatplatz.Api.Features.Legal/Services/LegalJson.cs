using System.Text.Json;

namespace Heimatplatz.Api.Features.Legal.Services;

/// <summary>
/// Serialisierung der LegalSettings-JSON-Spalten (ResponsiblePartyJson, SectionsJson).
///
/// Zentral, weil Seeder und Handler exakt dieselben Optionen brauchen: die Daten liegen
/// camelCase in der DB (so hat der Seeder sie urspruenglich geschrieben) - mit den
/// Default-Optionen wuerde das Deserialisieren still null liefern.
/// </summary>
internal static class LegalJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    public static T? Deserialize<T>(string? json) where T : class
        => string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<T>(json, Options);
}
