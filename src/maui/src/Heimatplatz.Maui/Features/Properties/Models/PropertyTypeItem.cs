using Heimatplatz.Maui.ApiClient.Generated;

namespace Heimatplatz.Maui.Features.Properties.Models;

/// <summary>
/// Helper-Klasse fuer die Anzeige von PropertyType im Picker.
/// Die Anzeigenamen liefert der Aufrufer lokalisiert mit (Models haben kein DI).
/// </summary>
public class PropertyTypeItem
{
    public required string DisplayName { get; init; }
    public required PropertyType Value { get; init; }

    public static List<PropertyTypeItem> GetAll(string houseLabel, string landLabel) =>
    [
        new PropertyTypeItem { DisplayName = houseLabel, Value = PropertyType.House },
        new PropertyTypeItem { DisplayName = landLabel, Value = PropertyType.Land }
    ];
}
