using System.Threading.Channels;

namespace Heimatplatz.Api.Features.AiListing.Infrastructure;

/// <summary>
/// In-Memory-Warteschlange fuer KI-Analyse-Jobs (Singleton).
/// Der ListingAnalysisWorker konsumiert die IDs im Hintergrund.
/// </summary>
public class ListingAnalysisQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(
        new UnboundedChannelOptions { SingleReader = true });

    public ValueTask EnqueueAsync(Guid analysisId, CancellationToken ct = default) =>
        _channel.Writer.WriteAsync(analysisId, ct);

    public IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken ct = default) =>
        _channel.Reader.ReadAllAsync(ct);
}
