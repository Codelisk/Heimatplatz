using Cake.Common.Diagnostics;

namespace Build.Tasks;

/// <summary>
/// Smoke-Check nach Deploys: pollt eine URL bis sie erfolgreich antwortet.
/// Ein Deploy-Task, der ohne diesen Check "gruen" endet, beweist nur, dass
/// Container gestartet wurden - nicht, dass die Anwendung antwortet.
/// </summary>
public static class DeployHealth
{
    public static void WaitFor(BuildContext context, string? url, int timeoutSeconds = 120)
    {
        if (string.IsNullOrEmpty(url))
        {
            context.Warning("Keine Health-URL konfiguriert - Smoke-Check uebersprungen.");
            return;
        }

        context.Information($"Health-Check: {url} (Timeout {timeoutSeconds}s)...");

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        Exception? lastError = null;

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var response = http.GetAsync(url).GetAwaiter().GetResult();
                if ((int)response.StatusCode < 400)
                {
                    context.Information($"Health OK: {url} -> HTTP {(int)response.StatusCode}");
                    return;
                }

                lastError = new InvalidOperationException($"HTTP {(int)response.StatusCode}");
            }
            catch (Exception ex)
            {
                lastError = ex;
            }

            Thread.Sleep(5_000);
        }

        throw new InvalidOperationException(
            $"Health-Check fuer {url} nach {timeoutSeconds}s fehlgeschlagen: {lastError?.Message}", lastError);
    }
}
