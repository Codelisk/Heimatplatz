using Heimatplatz.Maui.ApiClient.Generated;
using Microsoft.Extensions.Logging;
using Shiny.Mediator;

namespace Heimatplatz.Maui.Features.Properties.Services;

public enum ListingAnalysisRunState
{
    NotStarted,
    Running,
    Finished,
    Failed
}

/// <summary>
/// Orchestriert eine KI-Analyse im Hintergrund: Start-Request + Status-Polling
/// (2 s Intervall, 6 min Timeout - Poll-Loop aus dem frueheren AiAddPropertyViewModel).
/// Eine Instanz pro Wizard-Lauf; das ViewModel abonniert <see cref="StateChanged"/>
/// (Achtung: Events kommen von einem Hintergrund-Thread, UI-Zugriff via MainThread).
/// </summary>
public sealed class ListingAnalysisRunner(IMediator mediator, ILogger logger)
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan AnalysisTimeout = TimeSpan.FromMinutes(6);

    private CancellationTokenSource? _cts;

    public Guid? AnalysisId { get; private set; }
    public ListingAnalysisRunState State { get; private set; } = ListingAnalysisRunState.NotStarted;
    public ExtractedListingData? Result { get; private set; }
    public string? ErrorMessage { get; private set; }

    public event EventHandler? StateChanged;

    /// <summary>
    /// Startet die Analyse. Das Task-Ergebnis liegt vor, sobald die Analyse-Id bekannt
    /// ist (fuer die Entwurfs-Speicherung) - das Polling laeuft danach im Hintergrund weiter.
    /// Liefert null, wenn der Start fehlschlaegt (State ist dann Failed).
    /// </summary>
    public async Task<Guid?> StartAsync(List<string> imageUrls, List<string> videoUrls, string dictatedText)
    {
        Cancel();
        SetState(ListingAnalysisRunState.Running);
        _cts = new CancellationTokenSource(AnalysisTimeout);

        try
        {
            var (_, startResult) = await mediator.Request(
                new StartListingAnalysisHttpRequest
                {
                    Body = new StartListingAnalysisRequest
                    {
                        ImageUrls = imageUrls,
                        VideoUrls = videoUrls,
                        DictatedText = dictatedText.Trim()
                    }
                }, _cts.Token);

            if (startResult == null)
                throw new InvalidOperationException("Analyse konnte nicht gestartet werden.");

            AnalysisId = startResult.AnalysisId;
            _ = PollLoopAsync(startResult.AnalysisId, _cts.Token);
            return startResult.AnalysisId;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[ListingAnalysisRunner] Analyse-Start fehlgeschlagen");
            ErrorMessage = "Die KI-Analyse konnte nicht gestartet werden.";
            SetState(ListingAnalysisRunState.Failed);
            return null;
        }
    }

    /// <summary>
    /// Setzt das Polling einer bereits laufenden Analyse fort (Entwurf-Resume).
    /// Eine laengst abgeschlossene Analyse liefert sofort Finished samt Ergebnis.
    /// </summary>
    public void ResumePolling(Guid analysisId)
    {
        Cancel();
        AnalysisId = analysisId;
        SetState(ListingAnalysisRunState.Running);
        _cts = new CancellationTokenSource(AnalysisTimeout);
        _ = PollLoopAsync(analysisId, _cts.Token);
    }

    public void Cancel()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private async Task PollLoopAsync(Guid analysisId, CancellationToken ct)
    {
        try
        {
            while (true)
            {
                var (_, status) = await mediator.Request(
                    new GetListingAnalysisHttpRequest { AnalysisId = analysisId }, ct);

                if (status != null)
                {
                    if (status.Status == ListingAnalysisStatus.Finished && status.Result != null)
                    {
                        Result = status.Result;
                        SetState(ListingAnalysisRunState.Finished);
                        return;
                    }

                    if (status.Status == ListingAnalysisStatus.Failed
                        || (status.Status == ListingAnalysisStatus.Finished && status.Result == null))
                    {
                        ErrorMessage = string.IsNullOrEmpty(status.ErrorMessage)
                            ? "Die KI-Analyse ist fehlgeschlagen."
                            : status.ErrorMessage;
                        SetState(ListingAnalysisRunState.Failed);
                        return;
                    }
                }

                await Task.Delay(PollInterval, ct);
            }
        }
        catch (OperationCanceledException)
        {
            ErrorMessage = "Die KI-Analyse wurde abgebrochen oder hat zu lange gedauert.";
            SetState(ListingAnalysisRunState.Failed);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[ListingAnalysisRunner] Fehler beim Analyse-Polling");
            ErrorMessage = "Fehler bei der KI-Analyse.";
            SetState(ListingAnalysisRunState.Failed);
        }
    }

    private void SetState(ListingAnalysisRunState state)
    {
        State = state;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
