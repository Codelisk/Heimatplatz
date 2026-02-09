namespace Heimatplatz.Core.Startup.Services;

/// <summary>
/// Service zur Synchronisierung der Shiny-Initialisierung.
/// Ermoeglicht es Komponenten auf die Bereitschaft von Shiny zu warten.
/// </summary>
public interface IShinyReadinessService
{
    /// <summary>
    /// Wartet bis Shiny initialisiert ist.
    /// Auf Non-Mobile Plattformen kehrt sofort zurueck.
    /// </summary>
    Task WaitForReadyAsync();

    /// <summary>
    /// Signalisiert dass Shiny bereit ist.
    /// Wird von App.xaml.cs nach AndroidShinyHost.Init() aufgerufen.
    /// </summary>
    void SignalReady();
}
