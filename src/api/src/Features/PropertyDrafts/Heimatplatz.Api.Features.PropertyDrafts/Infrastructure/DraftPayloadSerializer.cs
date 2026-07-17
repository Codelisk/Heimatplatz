using Heimatplatz.Api.Features.PropertyDrafts.Contracts.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Heimatplatz.Api.Features.PropertyDrafts.Infrastructure;

/// <summary>
/// Zentrale (De-)Serialisierung des Entwurf-Payloads. Enums als String und
/// case-insensitive Properties, damit aeltere Entwuerfe auch nach Schema-Erweiterungen
/// tolerant gelesen werden koennen (unbekannte Felder ignoriert System.Text.Json ohnehin).
/// </summary>
internal static class DraftPayloadSerializer
{
    static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize(PropertyDraftData data)
        => JsonSerializer.Serialize(data, Options);

    public static PropertyDraftData Deserialize(string payloadJson)
    {
        try
        {
            return JsonSerializer.Deserialize<PropertyDraftData>(payloadJson, Options) ?? new PropertyDraftData();
        }
        catch (JsonException)
        {
            // Korrupter Payload darf das Laden der Entwurfsliste/des Wizards nicht crashen -
            // der Nutzer sieht dann einen leeren Entwurf und kann ihn loeschen.
            return new PropertyDraftData();
        }
    }
}
