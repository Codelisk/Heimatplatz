namespace Heimatplatz.Api.Features.WkoCompanies.Services;

/// <summary>
/// Parst die "Delta"-Antwort eines ASP.NET-AJAX-UpdatePanel-Postbacks (Sys.WebForms.PageRequestManager).
/// firmen.wko.at ist eine klassische ASP.NET-WebForms-Seite: die "Mehr laden"-Pagination laeuft NICHT
/// ueber einen simplen GET/Query-Parameter, sondern ueber einen POST mit __VIEWSTATE, der als Antwort
/// dieses laengenpraefixierte Format zurueckgibt (kein JSON, kein Standard-Format).
///
/// Format: eine Folge von Bloecken "&lt;contentLength&gt;|&lt;type&gt;|&lt;id&gt;|&lt;content&gt;|", wobei
/// contentLength die Zeichenlaenge von content ist (nicht der gesamten Zeile) - dadurch kann content
/// selbst beliebig viele "|" enthalten (z.B. HTML-Markup), ohne das Parsing zu verwirren.
/// Relevante Typen: "updatePanel" (id = Panel-ID, content = neues HTML) und "hiddenField"
/// (id = Feldname, content = neuer Wert - u.a. __VIEWSTATE/__VIEWSTATEGENERATOR/__EVENTVALIDATION
/// fuer den naechsten Postback).
/// </summary>
internal static class AspNetAjaxDeltaParser
{
    public sealed record DeltaBlock(string Type, string Id, string Content);

    public static List<DeltaBlock> Parse(string response)
    {
        var blocks = new List<DeltaBlock>();
        var pos = 0;
        var len = response.Length;

        while (pos < len)
        {
            var lengthEnd = response.IndexOf('|', pos);
            if (lengthEnd < 0) break;
            if (!int.TryParse(response.AsSpan(pos, lengthEnd - pos), out var contentLength))
                break;

            var typeEnd = response.IndexOf('|', lengthEnd + 1);
            if (typeEnd < 0) break;
            var type = response[(lengthEnd + 1)..typeEnd];

            var idEnd = response.IndexOf('|', typeEnd + 1);
            if (idEnd < 0) break;
            var id = response[(typeEnd + 1)..idEnd];

            var contentStart = idEnd + 1;
            if (contentStart + contentLength > len) break;
            var content = response.Substring(contentStart, contentLength);

            blocks.Add(new DeltaBlock(type, id, content));

            pos = contentStart + contentLength;
            if (pos < len && response[pos] == '|')
                pos++;
        }

        return blocks;
    }
}
