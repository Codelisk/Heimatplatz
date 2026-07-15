namespace Heimatplatz.Api.Core.AiConnectorClient.Configuration;

/// <summary>
/// Konfiguration fuer den generierten AiConnector-OpenAPI-Client.
/// Die Basis-URL wird nicht hier, sondern per Shiny-Mediator-Konvention ueber
/// "Mediator:Http:Heimatplatz.Api.Core.AiConnectorClient.Generated.*" konfiguriert.
/// </summary>
public class AiConnectorClientOptions
{
    public const string SectionName = "AiConnector";

    /// <summary>
    /// API-Key fuer den X-Api-Key Header des AiConnector-Backends.
    /// Nicht in appsettings.json committen, sondern per Env-Variable
    /// AiConnector__ApiKey setzen.
    /// </summary>
    public string ApiKey { get; set; } = "";
}
