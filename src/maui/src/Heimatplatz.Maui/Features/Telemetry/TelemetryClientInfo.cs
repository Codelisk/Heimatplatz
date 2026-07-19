namespace Heimatplatz.Maui.Features.Telemetry;

/// <summary>
/// Client-Identifikation fuer die Server-Telemetrie (X-Client-App-Header und
/// Ingestion-Metadaten). Alle Zugriffe sind crash-sicher gekapselt.
/// </summary>
public static class TelemetryClientInfo
{
    public const string ClientAppHeader = "X-Client-App";

    public static string AppVersion
    {
        get
        {
            try
            {
                return AppInfo.Current.VersionString;
            }
            catch
            {
                return "?";
            }
        }
    }

    public static string Platform
    {
        get
        {
            try
            {
                return DeviceInfo.Current.Platform.ToString();
            }
            catch
            {
                return "?";
            }
        }
    }

    /// <summary>Headerwert, z.B. "Maui/1.76.0 (Android)"</summary>
    public static string ClientApp => $"Maui/{AppVersion} ({Platform})";
}
