namespace Heimatplatz.Features.Properties.Contracts.Models.TypeSpecific;

/// <summary>
/// Bindable fields for plot-specific data
/// </summary>
public class PlotFields
{
    public bool IsBuildable { get; set; }
    public string? BuildablePercentage { get; set; }
    public string? ZoneType { get; set; }
    public bool HasUtilities { get; set; }
    public List<string> UtilityTypes { get; set; } = [];
    public string? SlopeType { get; set; }
}
