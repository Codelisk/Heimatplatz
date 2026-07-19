namespace Heimatplatz.Api.Features.Telemetry.Configuration;

/// <summary>
/// Konfiguration des Telemetry-Features (Section "Telemetry").
/// Alle Werte haben produktionstaugliche Defaults - die Section muss nicht existieren.
/// </summary>
public class TelemetryOptions
{
    public const string SectionName = "Telemetry";

    /// <summary>Schaltet die gesamte OTel-Pipeline (Prozessoren, Writer, Retention) ab</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Requests langsamer als dieser Schwellwert werden immer persistiert</summary>
    public int SlowRequestThresholdMs { get; set; } = 3000;

    /// <summary>Stichprobe gesunder Traces in Prozent (0-100) als Performance-Baseline</summary>
    public double SampleHealthyTracePercent { get; set; } = 2;

    public RetentionOptions RetentionDays { get; set; } = new();

    /// <summary>Max. gleichzeitig gepufferte Traces (Schutz vor Speicherwachstum)</summary>
    public int MaxBufferedTraces { get; set; } = 1000;

    /// <summary>Max. gepufferte Spans pro Trace (weitere werden verworfen)</summary>
    public int MaxSpansPerTrace { get; set; } = 200;

    /// <summary>Max. gepufferte Kontext-Logs (Info/Debug) pro Trace</summary>
    public int MaxLogsPerTrace { get; set; } = 200;

    /// <summary>Verwaiste Trace-Puffer aelter als dieser Wert werden aufgeraeumt</summary>
    public int AbandonedTraceTimeoutMinutes { get; set; } = 2;

    /// <summary>Writer-Flush-Intervall (Batch in die DB)</summary>
    public int WriterFlushIntervalSeconds { get; set; } = 3;

    /// <summary>Max. Zeilen pro Writer-Batch</summary>
    public int WriterMaxBatchSize { get; set; } = 500;

    /// <summary>Kapazitaet der Writer-Queue; bei Ueberlauf werden Eintraege verworfen (fail-open)</summary>
    public int WriterQueueCapacity { get; set; } = 10000;

    public ClientIngestionOptions ClientIngestion { get; set; } = new();

    public class RetentionOptions
    {
        public int Logs { get; set; } = 30;
        public int Spans { get; set; } = 14;
    }

    public class ClientIngestionOptions
    {
        /// <summary>Max. Eintraege pro Ingestion-Request (mehr ergibt 400)</summary>
        public int MaxBatchEntries { get; set; } = 20;

        /// <summary>Messages werden auf diese Laenge gekuerzt</summary>
        public int MaxMessageLength { get; set; } = 2000;

        /// <summary>Exception-Texte/Stacktraces werden auf diese Laenge gekuerzt</summary>
        public int MaxStackTraceLength { get; set; } = 20000;
    }
}
