namespace Heimatplatz.Features.Properties.Contracts.Interfaces;

/// <summary>
/// Service fuer Clipboard-Operationen
/// </summary>
public interface IClipboardService
{
    /// <summary>
    /// Kopiert einen Text in die Zwischenablage
    /// </summary>
    /// <param name="text">Der zu kopierende Text</param>
    /// <returns>True wenn erfolgreich, sonst false</returns>
    Task<bool> CopyToClipboardAsync(string text);
}
