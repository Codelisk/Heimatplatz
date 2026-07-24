using Shiny.Mediator;

namespace Heimatplatz.Api.Features.ForeclosureAuctions.Contracts.Mediator.Requests;

public record GetSyncStatusRequest : IRequest<GetSyncStatusResponse>;

public record GetSyncStatusResponse
{
    public DateTimeOffset? LastSyncAt { get; init; }
    public int TotalActiveAuctions { get; init; }
    public int TotalRemovedAuctions { get; init; }
    public int TotalChanges { get; init; }

    /// <summary>Ob gerade ein Sync-Lauf aktiv ist (Doppel-Trigger werden serverseitig ignoriert).</summary>
    public bool IsRunning { get; init; }
}
