namespace Heimatplatz.Api.Features.SearchConsole.Configuration;

/// <summary>
/// Konfiguration des SearchConsole-Features (Section "SearchConsole").
/// </summary>
public class SearchConsoleOptions
{
    public const string SectionName = "SearchConsole";

    /// <summary>
    /// Pfad zum Google-Service-Account-JSON-Key (Server-zu-Server-Auth, kein OAuth-Consent-Flow).
    /// Lokal relativ neben appsettings.json, im Container ein gemountetes Secret
    /// (siehe deploy/hetzner/docker-compose.yml, gleiches Muster wie Firebase/APNs).
    /// </summary>
    public string? ServiceAccountPath { get; set; }

    /// <summary>
    /// Search-Console-Property, fuer die abgefragt wird. Domain-Properties nutzen das
    /// "sc-domain:"-Praefix (z.B. "sc-domain:heimatplatz.at"), URL-prefix-Properties die
    /// volle URL (z.B. "https://heimatplatz.at/").
    /// </summary>
    public string SiteUrl { get; set; } = "sc-domain:heimatplatz.at";

    /// <summary>Fail-soft: ohne Key bleibt das Feature einfach "nicht konfiguriert".</summary>
    public bool Enabled => !string.IsNullOrWhiteSpace(ServiceAccountPath);
}
