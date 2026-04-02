namespace Heimatplatz.Features.Properties.Contracts.Models;

/// <summary>Eine gruppierte Sektion mit Header in der Property-Datentabelle.</summary>
public record PropertyDetailSection(
    string Title,
    PropertyDataCategory Category,
    List<PropertyDetailItem> Items
);
