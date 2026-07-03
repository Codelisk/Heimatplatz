using Microsoft.Extensions.Logging;
using Shiny;

namespace Heimatplatz.Maui.Features.Properties.Services;

/// <summary>
/// Service fuer Share-Operationen - nutzt den nativen MAUI Share-Dialog
/// oder die Zwischenablage als Fallback.
/// </summary>
[Singleton]
public class ShareService(
    IClipboardService clipboardService,
    ILogger<ShareService> logger
) : IShareService
{
    /// <inheritdoc />
    public Task<ShareResult> ShareTextAsync(string title, string text)
        => ShareAsync(title, text, null);

    /// <inheritdoc />
    public Task<ShareResult> ShareLinkAsync(string title, Uri uri, string? description = null)
        => ShareAsync(title, description, uri);

    private async Task<ShareResult> ShareAsync(string title, string? text, Uri? uri)
    {
        try
        {
            logger.LogInformation("Sharing: {Title}, HasText: {HasText}, HasUri: {HasUri}",
                title, !string.IsNullOrEmpty(text), uri != null);

            if (uri != null)
            {
                await Share.Default.RequestAsync(new ShareTextRequest
                {
                    Title = title,
                    Text = text,
                    Uri = uri.ToString()
                });
            }
            else
            {
                await Share.Default.RequestAsync(new ShareTextRequest
                {
                    Title = title,
                    Text = text
                });
            }

            return ShareResult.SharedNatively;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Nativer Share fehlgeschlagen, Fallback auf Zwischenablage");
            return await CopyToClipboardFallbackAsync(text, uri);
        }
    }

    private async Task<ShareResult> CopyToClipboardFallbackAsync(string? text, Uri? uri)
    {
        try
        {
            var contentParts = new List<string>();

            if (!string.IsNullOrEmpty(text))
                contentParts.Add(text);

            if (uri != null)
                contentParts.Add(uri.ToString());

            var content = string.Join("\n\n", contentParts);
            var success = await clipboardService.CopyToClipboardAsync(content);

            return success ? ShareResult.CopiedToClipboard : ShareResult.Failed;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Kopieren in die Zwischenablage fehlgeschlagen");
            return ShareResult.Failed;
        }
    }
}
