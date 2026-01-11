# Heimatplatz.Features.Immobilien.Contracts

Contracts und Models fuer das Immobilien-Feature im Uno Frontend.

## Models

### TechnicalFact
Key-Value Paar fuer technische Eigenschaften einer Immobilie.

```csharp
public record TechnicalFact(string Label, string Value);
```

## Abhaengigkeiten

- Heimatplatz.Shared (UnoService Konstanten)
