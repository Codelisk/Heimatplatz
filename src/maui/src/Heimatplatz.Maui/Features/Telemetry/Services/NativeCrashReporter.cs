#if IOS
using System.Text;
using System.Text.Json;
using Foundation;
using MetricKit;

namespace Heimatplatz.Maui.Features.Telemetry.Services;

/// <summary>
/// Meldet NATIVE Crashes (SIGSEGV, SIGABRT & Co.), die der managed
/// <see cref="CrashReporter"/> prinzipiell nicht fangen kann: MetricKit liefert
/// beim naechsten App-Start MXCrashDiagnostics inklusive Call-Stacks. Die werden
/// in den <see cref="CrashReportStore"/> uebersetzt und laufen damit ueber
/// denselben /api/telemetry/client-logs-Flow wie managed Crash-Reports.
/// Bewusst Apple-nativ (MetricKit) statt PLCrashReporter: kein zusaetzliches
/// natives Framework im Repo und keine In-Process-Signal-Handler, die mit der
/// Mono-Runtime kollidieren koennten (die nutzt SIGSEGV selbst fuer
/// NullReferenceExceptions).
/// </summary>
public static class NativeCrashReporter
{
    // Verhindert, dass ein einzelner MetricKit-Payload den 20er-Puffer flutet
    private const int MaxDiagnosticsPerPayload = 5;
    private const int MaxStackFrames = 40;

    private static MetricKitSubscriber? subscriber;

    /// <summary>
    /// Wird nach dem Persistieren neuer Reports aufgerufen (vom
    /// AppStartupService mit dem Upload verdrahtet). MetricKit liefert seine
    /// Payloads erst KURZ NACH dem App-Start - der regulaere
    /// SendPendingCrashReportsAsync-Aufruf beim Start ist dann schon durch.
    /// </summary>
    public static Action? OnReportsPersisted { get; set; }

    public static void Register()
    {
        if (subscriber != null)
            return;

        try
        {
            subscriber = new MetricKitSubscriber();
            MXMetricManager.SharedManager.Add(subscriber);
        }
        catch
        {
            // Crash-Reporting darf den App-Start nie gefaehrden
        }
    }

    private sealed class MetricKitSubscriber : NSObject, IMXMetricManagerSubscriber
    {
        [Export("didReceivePayloads:")]
        public void DidReceivePayloads(MXMetricPayload[] payloads)
        {
            // Metrik-Payloads werden nicht ausgewertet
        }

        [Export("didReceiveDiagnosticPayloads:")]
        public void DidReceiveDiagnosticPayloads(MXDiagnosticPayload[] payloads)
        {
            try
            {
                HandleDiagnostics(payloads);
            }
            catch
            {
                // fail-open wie der ganze Telemetrie-Client
            }
        }
    }

    private static void HandleDiagnostics(MXDiagnosticPayload[] payloads)
    {
        var persisted = false;

        foreach (var payload in payloads)
        {
            var crashes = payload.CrashDiagnostics;
            if (crashes is not { Length: > 0 })
                continue;

            var timestamp = ToTimestamp(payload.TimeStampEnd);

            foreach (var crash in crashes.Take(MaxDiagnosticsPerPayload))
            {
                CrashReportStore.Append(new CrashReportStore.PendingEntry(
                    TimestampUtc: timestamp,
                    Level: "Critical",
                    Message: $"Nativer App-Crash: {DescribeCrash(crash)}",
                    ExceptionText: BuildExceptionText(crash),
                    Screen: null));
                persisted = true;
            }
        }

        if (persisted)
            OnReportsPersisted?.Invoke();
    }

    private static DateTimeOffset ToTimestamp(NSDate? date)
    {
        try
        {
            if (date != null)
                return new DateTimeOffset(DateTime.SpecifyKind((DateTime)date, DateTimeKind.Utc));
        }
        catch
        {
            // NSDate ausserhalb des DateTime-Bereichs -> Fallback
        }

        return DateTimeOffset.UtcNow;
    }

    private static string DescribeCrash(MXCrashDiagnostic crash)
    {
        var exception = MachExceptionName(crash.ExceptionType);
        var signal = SignalName(crash.Signal);
        return signal is null ? exception : $"{exception} ({signal})";
    }

    private static string BuildExceptionText(MXCrashDiagnostic crash)
    {
        var sb = new StringBuilder();
        sb.AppendLine(DescribeCrash(crash));

        if (crash.ExceptionCode is not null)
            sb.AppendLine($"Exception-Code: {crash.ExceptionCode}");
        if (!string.IsNullOrEmpty(crash.TerminationReason))
            sb.AppendLine($"Termination-Reason: {crash.TerminationReason}");
        if (!string.IsNullOrEmpty(crash.VirtualMemoryRegionInfo))
            sb.AppendLine($"VM-Region: {crash.VirtualMemoryRegionInfo}");

        try
        {
            var meta = crash.MetaData;
            sb.AppendLine($"App {crash.ApplicationVersion} ({meta.ApplicationBuildVersion}), {meta.DeviceType}, iOS {meta.OSVersion}");
        }
        catch
        {
            // Metadaten sind nice-to-have
        }

        AppendCallStack(sb, crash);
        return sb.ToString();
    }

    /// <summary>
    /// Rendert aus dem CallStackTree-JSON den Stack des verursachenden Threads
    /// als "at {Binary} + 0x{Offset}"-Zeilen. Unsymbolisiert, aber zusammen mit
    /// Version/Binary-Offsets diagnostizierbar (gleiches Format wie Apple-Logs).
    /// </summary>
    private static void AppendCallStack(StringBuilder sb, MXCrashDiagnostic crash)
    {
        try
        {
            var data = crash.CallStackTree.JsonRepresentation;
            using var doc = JsonDocument.Parse(data.ToArray());

            if (!doc.RootElement.TryGetProperty("callStacks", out var callStacks) ||
                callStacks.ValueKind != JsonValueKind.Array)
                return;

            // Der verursachende Thread ist mit threadAttributed=true markiert
            JsonElement? attributed = null;
            foreach (var stack in callStacks.EnumerateArray())
            {
                if (stack.TryGetProperty("threadAttributed", out var attr) && attr.GetBoolean())
                {
                    attributed = stack;
                    break;
                }
                attributed ??= stack;
            }

            if (attributed is not { } thread ||
                !thread.TryGetProperty("callStackRootFrames", out var rootFrames) ||
                rootFrames.ValueKind != JsonValueKind.Array ||
                rootFrames.GetArrayLength() == 0)
                return;

            var frame = rootFrames[0];
            for (var depth = 0; depth < MaxStackFrames; depth++)
            {
                var binary = frame.TryGetProperty("binaryName", out var name) ? name.GetString() : null;
                var offset = frame.TryGetProperty("offsetIntoBinaryTextSegment", out var off) ? off.GetInt64() : 0;
                sb.AppendLine($"  at {binary ?? "?"} + 0x{offset:x}");

                if (!frame.TryGetProperty("subFrames", out var subFrames) ||
                    subFrames.ValueKind != JsonValueKind.Array ||
                    subFrames.GetArrayLength() == 0)
                    break;

                frame = subFrames[0];
            }
        }
        catch
        {
            // Ohne Stack bleibt der Report trotzdem wertvoll (Signal + Metadaten)
        }
    }

    private static string MachExceptionName(int? exceptionType) => exceptionType switch
    {
        1 => "EXC_BAD_ACCESS",
        2 => "EXC_BAD_INSTRUCTION",
        3 => "EXC_ARITHMETIC",
        4 => "EXC_EMULATION",
        5 => "EXC_SOFTWARE",
        6 => "EXC_BREAKPOINT",
        10 => "EXC_CRASH",
        11 => "EXC_RESOURCE",
        12 => "EXC_GUARD",
        13 => "EXC_CORPSE_NOTIFY",
        null => "EXC_UNKNOWN",
        _ => $"EXC_{exceptionType}"
    };

    private static string? SignalName(int? signal) => signal switch
    {
        4 => "SIGILL",
        5 => "SIGTRAP",
        6 => "SIGABRT",
        8 => "SIGFPE",
        9 => "SIGKILL",
        10 => "SIGBUS",
        11 => "SIGSEGV",
        13 => "SIGPIPE",
        null => null,
        _ => $"SIG{signal}"
    };
}
#endif
