using Heimatplatz.Maui.ApiClient.Generated;

namespace Heimatplatz.Maui.Features.Properties.Models;

/// <summary>
/// Helper-Klasse fuer die Anzeige von PropertyType im Picker
/// </summary>
public class PropertyTypeItem
{
    public required string DisplayName { get; init; }
    public required PropertyType Value { get; init; }

    public static List<PropertyTypeItem> GetAll() =>
    [
        new PropertyTypeItem { DisplayName = "Haus", Value = PropertyType.House },
        new PropertyTypeItem { DisplayName = "Grundstück", Value = PropertyType.Land }
    ];
}
