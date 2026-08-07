using System.Text;
using Heimatplatz.Api.Core.AiConnectorClient.Generated;
using Heimatplatz.Api.Features.Dashboards.Configuration;
using Heimatplatz.Api.Features.Dashboards.Contracts.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Dashboards.Services;

/// <summary>
/// Entwirft Dashboard-Definitionen ueber den externen AiConnector-Backend-Service.
/// Der Prompt laeuft im konfigurierten Workspace (Default: projects/heimatplatz),
/// referenziert die Dashboard-Section (sections/dashboard/AGENTS.md - Rolle, Ton,
/// Gestaltungsprinzipien) und traegt den Widget-Katalog + das Ausgabeformat SELBST
/// (zur Laufzeit aus den Resolver-Selbstbeschreibungen generiert - kein Drift
/// zwischen dem, was die KI kennt, und dem, was der Validator akzeptiert).
/// </summary>
public class AiConnectorDashboardDesigner(
    IMediator mediator,
    DashboardCatalogPromptBuilder catalogBuilder,
    IOptions<DashboardOptions> options,
    ILogger<AiConnectorDashboardDesigner> logger
) : IDashboardDesigner
{
    public async Task<string> DesignAsync(string request, string viewType, string? currentDefinitionJson, CancellationToken cancellationToken = default)
    {
        var opts = options.Value.AiConnector;
        var prompt = BuildPrompt(request, viewType, currentDefinitionJson, opts.SectionPath);

        logger.LogInformation("[Dashboards] Starte AiConnector-Generierung im Workspace {WorkspaceId} (Section {SectionPath}, ViewType={ViewType}, Refine={IsRefine})",
            opts.WorkspaceId, opts.SectionPath, viewType, currentDefinitionJson is not null);

        var response = await mediator.Request(new RunPromptHttpRequest
        {
            Body = new PromptRequest
            {
                Prompt = prompt,
                WorkspaceId = opts.WorkspaceId,
                Model = opts.Model
            }
        }, cancellationToken);

        var promptResponse = response.Result;

        if (!promptResponse.Success || string.IsNullOrWhiteSpace(promptResponse.Output))
        {
            if (promptResponse.TimedOut)
                throw new TimeoutException("Die Dashboard-Generierung über den AiConnector hat das Timeout überschritten.");
            throw new InvalidOperationException(
                $"AiConnector-Lauf fehlgeschlagen (ExitCode {promptResponse.ExitCode}): {Truncate(promptResponse.Error ?? "unbekannt", 500)}");
        }

        logger.LogInformation("[Dashboards] AiConnector-Antwort erhalten in {DurationMs}ms ({Length} Zeichen)",
            promptResponse.DurationMs, promptResponse.Output.Length);

        return promptResponse.Output;
    }

    private string BuildPrompt(string request, string viewType, string? currentDefinitionJson, string sectionPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Lies zuerst die Datei {sectionPath}/AGENTS.md in diesem Workspace und folge deren Regeln exakt.");
        sb.AppendLine();

        if (viewType == DashboardViewTypes.List)
        {
            sb.AppendLine("Entwirf eine persoenliche IMMOBILIENLISTE fuer einen Nutzer von Heimatplatz: eine");
            sb.AppendLine("seitenfuellende Trefferliste (genau EIN property-list-Widget, size full). Der Nutzer");
            sb.AppendLine("beschreibt, WONACH er sucht und WELCHE Werte er sehen moechte - uebersetze das in");
            sb.AppendLine("query + options.fields der Liste und optional eine detail-Spec fuers Anklicken.");
        }
        else
        {
            sb.AppendLine("Entwirf ein persoenliches DASHBOARD fuer einen Nutzer von Heimatplatz.");
            sb.AppendLine("Der Nutzer beschreibt, WONACH er sucht und WIE er es sehen moechte - uebersetze das in eine Widget-Komposition.");
        }
        sb.AppendLine();

        if (currentDefinitionJson is not null)
        {
            sb.AppendLine("Bestehende Ansicht (JSON) - baue sie gemaess dem Aenderungswunsch um, behalte Bewaehrtes bei:");
            sb.AppendLine(currentDefinitionJson);
            sb.AppendLine();
            sb.AppendLine("Aenderungswunsch des Nutzers:");
        }
        else
        {
            sb.AppendLine("Wunsch des Nutzers:");
        }

        sb.AppendLine("\"\"\"");
        sb.AppendLine(request.Trim());
        sb.AppendLine("\"\"\"");
        sb.AppendLine();

        sb.AppendLine(catalogBuilder.BuildCatalogSection(viewType));
        sb.AppendLine(catalogBuilder.BuildOutputContractSection(viewType));

        return sb.ToString();
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength] + "…";
}
