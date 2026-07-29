using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Heimatplatz.Maui.Http;

/// <summary>
/// Persistiert pro Stammdaten-URL das Paar aus ETag und Antwort-Body fuer
/// Conditional GETs (<see cref="StammdatenConditionalGetHandler"/>). Eine Datei
/// pro URL, der Dateiname ist ein Hash ueber Host+Pfad - ein Wechsel des
/// API-Endpunkts (Prod/Test) trennt die Eintraege dadurch automatisch.
/// Fehler werden verschluckt (Cache ist optional): ohne Eintrag laeuft der
/// Request einfach als normaler unkonditionaler GET.
/// </summary>
public sealed class ConditionalGetStore(ILogger<ConditionalGetStore> logger)
{
    public sealed record Entry(string ETag, string Body);

    private readonly string _directory =
        Path.Combine(FileSystem.Current.AppDataDirectory, "conditional-get");

    public async Task<Entry?> GetAsync(Uri requestUri, CancellationToken ct)
    {
        try
        {
            var path = GetFilePath(requestUri);
            if (!File.Exists(path))
                return null;

            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<Entry>(stream, cancellationToken: ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Conditional-GET-Eintrag fuer {Uri} nicht lesbar", requestUri);
            return null;
        }
    }

    public async Task SetAsync(Uri requestUri, Entry entry, CancellationToken ct)
    {
        try
        {
            Directory.CreateDirectory(_directory);
            var path = GetFilePath(requestUri);

            // Temp-Datei + Move statt direktem Schreiben: eine abgebrochene App
            // hinterlaesst so nie einen halb geschriebenen (unparsebaren) Eintrag
            var tempPath = path + ".tmp";
            await using (var stream = File.Create(tempPath))
            {
                await JsonSerializer.SerializeAsync(stream, entry, cancellationToken: ct);
            }

            File.Move(tempPath, path, overwrite: true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Conditional-GET-Eintrag fuer {Uri} nicht speicherbar", requestUri);
        }
    }

    private string GetFilePath(Uri uri)
    {
        var identity = $"{uri.Authority}{uri.PathAndQuery}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
        return Path.Combine(_directory, hash + ".json");
    }
}
