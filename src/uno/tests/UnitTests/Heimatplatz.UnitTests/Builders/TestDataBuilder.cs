namespace Heimatplatz.UnitTests.Builders;

/// <summary>
/// Basis-Builder fuer Fluent Test-Data-Erstellung.
/// </summary>
/// <typeparam name="TBuilder">Der konkrete Builder-Typ.</typeparam>
/// <typeparam name="TResult">Der Ergebnis-Typ.</typeparam>
public abstract class TestDataBuilder<TBuilder, TResult>
    where TBuilder : TestDataBuilder<TBuilder, TResult>
{
    /// <summary>
    /// Baut das Objekt.
    /// </summary>
    /// <returns>Das erstellte Objekt.</returns>
    public abstract TResult Build();

    /// <summary>
    /// Gibt den aktuellen Builder zurueck (fuer Fluent-API).
    /// </summary>
    protected TBuilder This => (TBuilder)this;
}
