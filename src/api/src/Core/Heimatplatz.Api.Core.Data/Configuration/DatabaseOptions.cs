namespace Heimatplatz.Api.Core.Data.Configuration;

/// <summary>
/// Konfigurationsoptionen für die Datenbank-Initialisierung.
/// Wird über die "Database"-Sektion in appsettings.json konfiguriert.
/// </summary>
public class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>
    /// Wenn true, werden Seeder beim App-Start ausgeführt.
    /// Sollte nur in Development aktiviert sein.
    /// </summary>
    public bool EnableSeeding { get; set; }

    /// <summary>
    /// Wenn true, werden EF Core Migrations beim Start automatisch angewendet.
    /// In Production sollte dies false sein (manuelle Migration vor Deployment).
    /// </summary>
    public bool AutoMigrate { get; set; }
}
