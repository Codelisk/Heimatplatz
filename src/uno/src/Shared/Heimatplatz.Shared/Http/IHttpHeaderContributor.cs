namespace Heimatplatz.Http;

/// <summary>
/// Contributes headers to outgoing HTTP requests.
/// Implement this interface in features that need to add custom headers.
/// </summary>
/// <remarks>
/// Contributors are executed in order of their <see cref="Priority"/> (lower values first).
/// If multiple contributors set the same header, later contributors will overwrite earlier values.
/// </remarks>
public interface IHttpHeaderContributor
{
    /// <summary>
    /// Execution priority. Lower values execute first.
    /// </summary>
    /// <remarks>
    /// Recommended priority ranges:
    /// - Authentication: 0-10
    /// - Telemetry/Correlation: 100-199
    /// - Custom headers: 200+
    /// </remarks>
    int Priority => 100;

    /// <summary>
    /// Adds headers to the outgoing HTTP request.
    /// </summary>
    /// <param name="request">The HTTP request message to modify.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ContributeAsync(HttpRequestMessage request, CancellationToken ct = default);
}
