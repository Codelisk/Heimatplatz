namespace Heimatplatz.Features.Properties.Contracts.Models;

public record PropertyDetailItem(
    string Label,
    string Value,
    PropertyDataCategory Category,
    bool Highlighted = false
);
