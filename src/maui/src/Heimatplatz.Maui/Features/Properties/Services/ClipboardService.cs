using Shiny;

namespace Heimatplatz.Maui.Features.Properties.Services;

/// <summary>
/// Service fuer Clipboard-Operationen (MAUI Clipboard)
/// </summary>
[Singleton]
public class ClipboardService : IClipboardService
{
    /// <inheritdoc />
    public async Task<bool> CopyToClipboardAsync(string text)
    {
        try
        {
            await Clipboard.Default.SetTextAsync(text);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
