namespace Heimatplatz.Features.Immobilien.Contracts.Models;

/// <summary>
/// Key-Value Paar fuer technische Eigenschaften einer Immobilie.
/// Wird in der Detailansicht unter "Technical Facts" angezeigt.
/// </summary>
public record TechnicalFact(string Label, string Value);
