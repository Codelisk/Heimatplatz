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

    /// <summary>
    /// Wenn true, wird die Datenbank beim Start geloescht und neu erstellt.
    /// Nützlich bei Schema-Aenderungen wenn keine Migrations vorhanden sind.
    /// ACHTUNG: Alle Daten gehen verloren! Nach einmaligem Einsatz wieder auf false setzen.
    /// </summary>
    public bool ForceRecreate { get; set; }
}
