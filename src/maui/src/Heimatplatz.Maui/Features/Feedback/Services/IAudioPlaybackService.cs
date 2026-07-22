namespace Heimatplatz.Maui.Features.Feedback.Services;

/// <summary>
/// Spielt lokale Audio-Dateien ab (Sprachnachrichten, WAV/M4A). Bewusst ein eigener
/// kleiner Plattform-Player (Android MediaPlayer / iOS AVAudioPlayer) statt Shinys
/// IAudioPlayer - der ist laut Doku auf MP3-Streams ausgelegt.
/// </summary>
public interface IAudioPlaybackService
{
    bool IsSupported { get; }

    bool IsPlaying { get; }

    /// <summary>Wird auch bei Stop/Fehler gefeuert, damit die UI den Play-Zustand zuruecksetzt.</summary>
    event EventHandler? PlaybackEnded;

    /// <summary>Startet die Wiedergabe (stoppt eine laufende vorher). False bei Fehler.</summary>
    Task<bool> PlayFileAsync(string filePath);

    Task StopAsync();
}
