using Shiny.Mediator;

namespace Heimatplatz.Api.Features.SearchConsole.Contracts.Mediator.Requests;

/// <summary>
/// Suchperformance-Uebersicht der letzten 28 Tage (mit ein paar Tagen Verzoegerung,
/// wie von der Search-Console-API vorgegeben) fuer die konfigurierte Property.
/// </summary>
public record GetSearchConsoleSummaryRequest : IRequest<GetSearchConsoleSummaryResponse>;

/// <summary>
/// Fail-soft: <see cref="Enabled"/> ist false, solange kein Service-Account-Key
/// konfiguriert ist - dann sind alle Zahlenfelder 0 und die Listen leer.
/// </summary>
public record GetSearchConsoleSummaryResponse
{
    public required bool Enabled { get; init; }
    public int ClicksTotal { get; init; }
    public int ImpressionsTotal { get; init; }
    public double AverageCtr { get; init; }
    public double AveragePosition { get; init; }
    public List<SearchConsoleRowDto> TopQueries { get; init; } = [];
    public List<SearchConsoleRowDto> TopPages { get; init; } = [];
}

/// <summary>Eine Zeile aus der Search-Console-Suchanalyse (pro Suchbegriff oder Seite).</summary>
public record SearchConsoleRowDto
{
    public required string Label { get; init; }
    public int Clicks { get; init; }
    public int Impressions { get; init; }
    public double Ctr { get; init; }
    public double Position { get; init; }
}
