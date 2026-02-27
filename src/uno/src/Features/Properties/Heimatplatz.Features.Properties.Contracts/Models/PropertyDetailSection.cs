namespace Heimatplatz.Features.Properties.Contracts.Models;

public record PropertyDetailSection(
    string Title,
    PropertyDataCategory Category,
    List<PropertyDetailItem> Items
);
