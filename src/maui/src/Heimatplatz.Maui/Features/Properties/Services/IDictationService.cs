namespace Heimatplatz.Maui.Features.Properties.Services;

/// <summary>
/// Diktat-Abstraktion fuer die KI-gestuetzte Inseratserstellung.
/// Auf Android/iOS via Shiny.Speech implementiert; auf anderen Plattformen nicht verfuegbar
/// (dort ist der manuelle Erfassungsweg der Standard).
/// </summary>
public interface IDictationService
{
    /// <summary>True wenn Diktat auf dieser Plattform verfuegbar ist (Android/iOS)</summary>
    bool IsSupported { get; }

    /// <summary>True solange ein Diktat aktiv ist (inkl. automatischer Neustarts nach Sprechpausen)</summary>
    bool IsListening { get; }

    /// <summary>Laufender Zwischenstand des aktuellen Sprachsegments</summary>
    event EventHandler<string>? PartialResult;

    /// <summary>Ein abgeschlossenes Sprachsegment (an den Text anhaengen)</summary>
    event EventHandler<string>? FinalResult;

    /// <summary>Fehler bei der Erkennung (Meldung fuer den Nutzer)</summary>
    event EventHandler<string>? Failed;

    /// <summary>Diktat wurde beendet (manuell oder durch Fehler)</summary>
    event EventHandler? Stopped;

    /// <summary>Fordert Mikrofon-/Spracherkennungs-Berechtigung an</summary>
    Task<bool> RequestPermissionAsync();

    /// <summary>Startet das Diktat (kontinuierlich, ueberlebt Sprechpausen)</summary>
    Task StartAsync();

    /// <summary>Beendet das Diktat</summary>
    Task StopAsync();
}
