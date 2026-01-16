using Heimatplatz.Features.Properties.Contracts.Models;

namespace Heimatplatz.Features.Properties.Models;

/// <summary>
/// Helper class for displaying PropertyType in ComboBox
/// </summary>
public class PropertyTypeItem
{
    public required string DisplayName { get; init; }
    public required PropertyType Value { get; init; }

    public static List<PropertyTypeItem> GetAll() => new()
    {
        new PropertyTypeItem { DisplayName = "Haus", Value = PropertyType.House },
        new PropertyTypeItem { DisplayName = "Grundstück", Value = PropertyType.Land }
    };
}
