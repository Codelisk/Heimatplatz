using System.Text.Json;
using System.Text.Json.Serialization;
using Heimatplatz.Api.Features.Dashboards.Contracts.Models;

namespace Heimatplatz.Api.Features.Dashboards.Infrastructure;

/// <summary>
/// Zentrale (De-)Serialisierung der DashboardDefinition: camelCase wie die
/// KI-Ausgabe und die Web-Clients, tolerant beim Lesen (case-insensitive,
/// unbekannte Felder ignorieren). Wird fuer DB-Spalten UND fuer das Parsen
/// der KI-Ausgabe verwendet - eine Definition, ein Format.
/// </summary>
public static class DashboardDefinitionSerializer
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public static string Serialize(DashboardDefinition definition) =>
        JsonSerializer.Serialize(definition, JsonOptions);

    /// <summary>
    /// Deserialisiert eine gespeicherte Definition. Defektes JSON liefert null
    /// (tolerant - der Aufrufer behandelt das wie "keine Definition").
    /// </summary>
    public static DashboardDefinition? DeserializeStored(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        try
        {
            return JsonSerializer.Deserialize<DashboardDefinition>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
