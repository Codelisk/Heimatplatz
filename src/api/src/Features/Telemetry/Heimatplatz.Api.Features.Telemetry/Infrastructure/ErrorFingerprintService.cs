using System.Security.Cryptography;
using System.Text;

namespace Heimatplatz.Api.Features.Telemetry.Infrastructure;

/// <summary>
/// Berechnet stabile Fingerprints fuer Exceptions: SHA-256 ueber Exception-Typ +
/// normalisierte Top-Stackframes (Dateipfade/Zeilennummern gestrippt) + Message-Template.
/// Gleiche Fehlerursache ergibt so denselben Hash, auch wenn Message-Werte,
/// Build-Pfade oder Zeilennummern variieren.
/// </summary>
public class ErrorFingerprintService
{
    private const int MaxFrames = 5;
    private const int MaxTitleLength = 512;

    public string Fingerprint(string exceptionType, string? stackTrace, string? messageTemplate)
    {
        var builder = new StringBuilder(exceptionType);

        foreach (var frame in NormalizedTopFrames(stackTrace))
        {
            builder.Append('\n').Append(frame);
        }

        if (!string.IsNullOrEmpty(messageTemplate))
        {
            builder.Append('\n').Append(messageTemplate);
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexStringLower(hash);
    }

    /// <summary>
    /// Kurzbezeichnung fuer Listen: "Typ: erste Zeile der Message", auf 512 Zeichen gekuerzt.
    /// </summary>
    public string BuildTitle(string exceptionType, string message)
    {
        var firstLine = message.AsSpan();
        var newline = firstLine.IndexOfAny('\r', '\n');
        if (newline >= 0)
        {
            firstLine = firstLine[..newline];
        }

        var title = $"{exceptionType}: {firstLine}";
        return title.Length <= MaxTitleLength ? title : title[..MaxTitleLength];
    }

    /// <summary>
    /// Zerlegt einen Client-Exception-Text (Exception.ToString()) in Typ, Message und
    /// Stacktrace. Erste Zeile hat das Format "Namespace.Typ: Message"; ohne erkennbaren
    /// Typ wird "ClientError" verwendet.
    /// </summary>
    public (string Type, string Message, string? StackTrace) ParseExceptionText(string exceptionText)
    {
        var text = exceptionText.ReplaceLineEndings("\n");
        var firstNewline = text.IndexOf('\n');
        var firstLine = firstNewline >= 0 ? text[..firstNewline] : text;
        var rest = firstNewline >= 0 ? text[(firstNewline + 1)..] : null;

        var colon = firstLine.IndexOf(": ", StringComparison.Ordinal);
        if (colon > 0 && !firstLine.AsSpan(0, colon).ContainsAny(' ', '\t'))
        {
            return (firstLine[..colon], firstLine[(colon + 2)..], rest);
        }

        return ("ClientError", firstLine, rest);
    }

    /// <summary>
    /// Liefert die obersten Frames ("at ..."/"bei ..." je nach OS-Sprache) ohne den
    /// " in Datei:Zeile"-Teil - Zeilennummern und Build-Pfade duerfen den Hash nicht aendern.
    /// </summary>
    private static IEnumerable<string> NormalizedTopFrames(string? stackTrace)
    {
        if (string.IsNullOrWhiteSpace(stackTrace))
            yield break;

        var count = 0;
        foreach (var raw in stackTrace.Split('\n'))
        {
            var line = raw.Trim();
            if (line.StartsWith("at ", StringComparison.Ordinal))
            {
                line = line[3..];
            }
            else if (line.StartsWith("bei ", StringComparison.Ordinal))
            {
                line = line[4..];
            }
            else
            {
                continue;
            }

            var inIndex = line.IndexOf(" in ", StringComparison.Ordinal);
            if (inIndex > 0)
            {
                line = line[..inIndex];
            }

            yield return line;

            if (++count >= MaxFrames)
                yield break;
        }
    }
}
