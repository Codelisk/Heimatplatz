namespace Heimatplatz.Maui.Features.Feedback.Services;

/// <summary>Fertige Sprachaufnahme als WAV-Datei im App-Cache.</summary>
public record VoiceRecording(string FilePath, double DurationSeconds, long FileSizeBytes);

/// <summary>
/// Nimmt Sprachnachrichten als WAV-Datei auf (Shiny IAudioSource, 16 kHz mono).
/// Nur auf Android/iOS verfuegbar - auf anderen Plattformen ist IsSupported false
/// und die Aufnahme-UI bleibt ausgeblendet.
/// </summary>
public interface IVoiceRecorderService
{
    bool IsSupported { get; }

    bool IsRecording { get; }

    /// <summary>Bereits aufgenommene Dauer der laufenden Aufnahme.</summary>
    TimeSpan Elapsed { get; }

    Task<bool> RequestAccessAsync();

    Task StartAsync();

    /// <summary>Beendet die Aufnahme und liefert die fertige WAV-Datei (null bei leerer Aufnahme).</summary>
    Task<VoiceRecording?> StopAsync();

    /// <summary>Bricht die Aufnahme ab und verwirft die Datei.</summary>
    Task CancelAsync();
}
