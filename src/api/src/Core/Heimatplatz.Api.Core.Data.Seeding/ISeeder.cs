namespace Heimatplatz.Api.Core.Data.Seeding;

/// <summary>
/// Interface for feature seeders that populate the database with test data.
/// Implement this interface in each feature project that needs seed data.
/// </summary>
public interface ISeeder
{
    /// <summary>
    /// Order in which this seeder runs. Lower numbers run first.
    /// Use this when seeders have dependencies on each other.
    /// </summary>
    int Order => 0;

    /// <summary>
    /// Seeds the database with test data.
    /// Should be idempotent - check if data exists before inserting.
    /// </summary>
    Task SeedAsync(CancellationToken cancellationToken = default);
}
