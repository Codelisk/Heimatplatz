using System.Text;
using Heimatplatz.Api;
using Heimatplatz.Api.Features.Dashboards.Configuration;
using Heimatplatz.Api.Features.Dashboards.Services.Widgets;
using Microsoft.Extensions.Options;
using Shiny;

namespace Heimatplatz.Api.Features.Dashboards.Services;

/// <summary>
/// Rendert den Widget-Katalog zur Laufzeit aus den Resolver-Selbstbeschreibungen
/// in den KI-Prompt. Der Katalog, den die KI sieht, ist damit IMMER identisch mit
/// dem, was der Validator akzeptiert - eine Quelle der Wahrheit im Code, kein
/// Sync mit dem Workspace, kein Drift. Neue Widget-Arten erscheinen automatisch.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class DashboardCatalogPromptBuilder(
    IEnumerable<IDashboardWidgetResolver> resolvers,
    IOptions<DashboardOptions> options
)
{
    public string BuildCatalogSection()
    {
        var limits = options.Value.Limits;
        var sb = new StringBuilder();

        sb.AppendLine("WIDGET-KATALOG (nur diese kind-Werte sind erlaubt):");
        foreach (var resolver in resolvers)
        {
            var d = resolver.Descriptor;
            sb.AppendLine($"- \"{d.Kind}\": {d.Purpose} {d.Details}");
        }
        sb.AppendLine();

        sb.AppendLine("GEMEINSAMES query-OBJEKT (fuer alle Widgets ausser text-note; nur gesetzte Felder ausgeben):");
        sb.AppendLine("{");
        sb.AppendLine("  \"types\": [\"house\"|\"land\"|\"foreclosure\"],   // leer = Haus + Grundstueck; \"foreclosure\" (Zwangsversteigerungen) NUR wenn ausdruecklich gewuenscht");
        sb.AppendLine("  \"sellers\": [\"private\"|\"broker\"],           // leer = alle Anbieter");
        sb.AppendLine("  \"locations\": [\"Gemeinde- oder Bezirksname\"], // im Wortlaut des Nutzers, KEINE IDs - die Aufloesung passiert serverseitig");
        sb.AppendLine("  \"priceMin\": Zahl, \"priceMax\": Zahl,          // Euro");
        sb.AppendLine("  \"areaMin\": Zahl, \"areaMax\": Zahl,            // m² (Wohnflaeche, bei Grundstuecken Grundflaeche)");
        sb.AppendLine("  \"roomsMin\": Zahl,");
        sb.AppendLine("  \"includeNewBuild\": false,                    // nur setzen wenn Neubauprojekte ausgeblendet werden sollen");
        sb.AppendLine("  \"searchText\": \"Volltext\",                   // nur fuer Begriffe, die kein anderes Feld abdeckt");
        sb.AppendLine("  \"sort\": \"newest\"|\"oldest\"|\"price-asc\"|\"price-desc\"|\"area-asc\"|\"area-desc\",");
        sb.AppendLine($"  \"limit\": Zahl                                // 1-{limits.MaxListItems}");
        sb.AppendLine("}");

        return sb.ToString();
    }

    public string BuildOutputContractSection()
    {
        var limits = options.Value.Limits;
        var sb = new StringBuilder();

        sb.AppendLine("AUSGABEFORMAT - antworte AUSSCHLIESSLICH mit EINEM JSON-Objekt, keine Codezaeune, kein Text davor oder danach:");
        sb.AppendLine("{");
        sb.AppendLine("  \"schemaVersion\": 1,");
        sb.AppendLine("  \"title\": \"kurzer Titel der Uebersicht\",");
        sb.AppendLine("  \"intro\": \"ein Einleitungssatz (optional)\",");
        sb.AppendLine("  \"widgets\": [");
        sb.AppendLine("    { \"id\": \"w1\", \"kind\": \"...\", \"size\": \"s\"|\"m\"|\"l\"|\"full\", \"title\": \"...\", \"query\": {...}, \"options\": {...} }");
        sb.AppendLine("  ],");
        sb.AppendLine("  \"unsupportedWishes\": [\"Wunsch, den kein Widget abdecken kann\"]");
        sb.AppendLine("}");
        sb.AppendLine("Regeln:");
        sb.AppendLine($"- Maximal {limits.MaxWidgets} Widgets; so wenige wie noetig, Reihenfolge = Wichtigkeit fuer den Nutzer.");
        sb.AppendLine("- NICHTS erfinden: Wuensche, die der Katalog nicht abdeckt, gehoeren nach unsupportedWishes.");
        sb.AppendLine("- Optik (Farben, Schriften, Layout-Details) bestimmt die Plattform - dafuer gibt es keine Felder.");
        sb.AppendLine("- title/intro/options.text auf Deutsch in der Sie-Form.");

        return sb.ToString();
    }
}
