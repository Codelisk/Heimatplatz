using Heimatplatz.Features.Properties.Contracts.Interfaces;
using Shiny.Extensions.DependencyInjection;
using Windows.ApplicationModel.DataTransfer;

namespace Heimatplatz.Features.Properties.Services;

/// <summary>
/// Service fuer Clipboard-Operationen
/// </summary>
[Service(UnoService.Lifetime, TryAdd = UnoService.TryAdd)]
public class ClipboardService : IClipboardService
{
    /// <inheritdoc />
    public Task<bool> CopyToClipboardAsync(string text)
    {
        try
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(text);
            Clipboard.SetContent(dataPackage);
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }
}
