namespace Heimatplatz.Api.Features.AiListing.Configuration;

/// <summary>
/// Konfiguration fuer die KI-gestuetzte Inseratserstellung.
/// </summary>
public class AiListingOptions
{
    public const string SectionName = "AiListing";

    /// <summary>
    /// Welcher Extraktions-Provider verwendet wird:
    /// "Mock" (Dev, heuristische Extraktion ohne KI) oder
    /// "Cli" (Server: ruft eine installierte Agent-CLI wie claude oder codex auf).
    /// </summary>
    public string Provider { get; set; } = "Mock";

    /// <summary>Executable der Agent-CLI, z.B. "claude" oder "codex"</summary>
    public string CliCommand { get; set; } = "claude";

    /// <summary>
    /// Argumente fuer die CLI. Der Prompt wird ueber stdin uebergeben.
    /// Beispiel claude: "-p --output-format text"
    /// </summary>
    public string CliArguments { get; set; } = "-p --output-format text";

    /// <summary>Arbeitsverzeichnis fuer den CLI-Prozess (leer = aktuelles Verzeichnis)</summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>Timeout fuer eine einzelne Analyse</summary>
    public int TimeoutSeconds { get; set; } = 300;

    /// <summary>Maximale Anzahl Fotos pro Analyse</summary>
    public int MaxImages { get; set; } = 20;

    /// <summary>Maximale Anzahl Videos pro Analyse</summary>
    public int MaxVideos { get; set; } = 3;

    /// <summary>Maximale Groesse eines Videos in MB</summary>
    public int MaxVideoSizeMb { get; set; } = 60;
}
