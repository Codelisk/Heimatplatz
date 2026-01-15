namespace Heimatplatz.Api.UnitTests.Builders;

/// <summary>
/// Basis-Builder fuer Fluent Entity-Erstellung in Tests.
/// </summary>
/// <typeparam name="TBuilder">Der konkrete Builder-Typ.</typeparam>
/// <typeparam name="TEntity">Der Entity-Typ.</typeparam>
public abstract class EntityBuilder<TBuilder, TEntity>
    where TBuilder : EntityBuilder<TBuilder, TEntity>
    where TEntity : class
{
    /// <summary>
    /// Baut die Entity.
    /// </summary>
    /// <returns>Die erstellte Entity.</returns>
    public abstract TEntity Build();

    /// <summary>
    /// Gibt den aktuellen Builder zurueck (fuer Fluent-API).
    /// </summary>
    protected TBuilder This => (TBuilder)this;
}
