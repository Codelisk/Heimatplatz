using Shiny.Mediator;

namespace Heimatplatz.Api.Features.PropertyImport.Contracts.Mediator.Requests;

/// <summary>
/// Request zum Batch-Import von Properties
/// </summary>
/// <param name="Properties">Liste der zu importierenden Properties</param>
/// <param name="UpdateExisting">Bei true werden existierende Properties aktualisiert, bei false uebersprungen</param>
public record ImportPropertiesRequest(
    List<ImportPropertyDto> Properties,
    bool UpdateExisting = true
) : IRequest<ImportPropertiesResponse>;

/// <summary>
/// Response des Batch-Imports
/// </summary>
public record ImportPropertiesResponse(
    /// <summary>Anzahl empfangener Properties</summary>
    int TotalReceived,

    /// <summary>Anzahl neu erstellter Properties</summary>
    int Created,

    /// <summary>Anzahl aktualisierter Properties</summary>
    int Updated,

    /// <summary>Anzahl uebersprungener Properties (existierten bereits)</summary>
    int Skipped,

    /// <summary>Anzahl fehlgeschlagener Imports</summary>
    int Failed,

    /// <summary>Detaillierte Ergebnisse pro Property</summary>
    List<ImportResultItem> Results
);

/// <summary>
/// Ergebnis fuer eine einzelne importierte Property
/// </summary>
public record ImportResultItem(
    /// <summary>SourceId der Property</summary>
    string SourceId,

    /// <summary>Status des Imports</summary>
    ImportResultStatus Status,

    /// <summary>ID der Property (bei Erfolg)</summary>
    Guid? PropertyId = null,

    /// <summary>Fehlermeldung (bei Fehler)</summary>
    string? ErrorMessage = null
);

/// <summary>
/// Status eines einzelnen Imports
/// </summary>
public enum ImportResultStatus
{
    /// <summary>Property wurde neu erstellt</summary>
    Created,

    /// <summary>Existierende Property wurde aktualisiert</summary>
    Updated,

    /// <summary>Property wurde uebersprungen (existierte bereits und UpdateExisting=false)</summary>
    Skipped,

    /// <summary>Import ist fehlgeschlagen</summary>
    Failed
}
