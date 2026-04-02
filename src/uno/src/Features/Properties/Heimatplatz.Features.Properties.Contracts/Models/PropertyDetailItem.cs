namespace Heimatplatz.Features.Properties.Contracts.Models;

/// <summary>Eine einzelne Zeile in der Property-Datentabelle (Label-Value-Paar).</summary>
public record PropertyDetailItem(
    string Label,
    string Value,
    PropertyDataCategory Category,
    bool Highlighted = false
);
