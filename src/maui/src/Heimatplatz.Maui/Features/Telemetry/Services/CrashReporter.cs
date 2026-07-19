namespace Heimatplatz.Maui.Features.Telemetry.Services;

/// <summary>
/// Globale Crash-Hooks: unbehandelte Exceptions (Prozess-Tod) und unbeobachtete
/// Task-Exceptions werden in die Preferences persistiert (<see cref="CrashReportStore"/>)
/// und beim naechsten Start via /api/telemetry/client-logs gemeldet.
/// Muss so frueh wie moeglich in MauiProgram.CreateMauiApp registriert werden.
/// </summary>
public static class CrashReporter
{
    private static int registered;

    public static void RegisterGlobalHandlers()
    {
        if (Interlocked.Exchange(ref registered, 1) == 1)
            return;

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            Capture(args.ExceptionObject as Exception, fatal: true);

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Capture(args.Exception, fatal: false);
            args.SetObserved();
        };
    }

    private static void Capture(Exception? exception, bool fatal)
    {
        if (exception == null)
            return;

        try
        {
            CrashReportStore.Append(new CrashReportStore.PendingEntry(
                TimestampUtc: DateTimeOffset.UtcNow,
                Level: fatal ? "Critical" : "Error",
                Message: fatal
                    ? $"App-Crash: {exception.Message}"
                    : $"Unbeobachtete Task-Exception: {exception.Message}",
                ExceptionText: exception.ToString(),
                Screen: TryGetCurrentScreen()));
        }
        catch
        {
            // Crash-Handler darf nie selbst werfen
        }
    }

    private static string? TryGetCurrentScreen()
    {
        try
        {
            // Kann ausserhalb des Main-Threads fehlschlagen - dann ohne Screen melden
            return Shell.Current?.CurrentState?.Location?.ToString();
        }
        catch
        {
            return null;
        }
    }
}
