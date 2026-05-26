using Heimatplatz.Features.Properties.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Shiny;
using Windows.ApplicationModel.DataTransfer;

namespace Heimatplatz.Features.Properties.Services;

/// <summary>
/// Service fuer Share-Operationen - nutzt nativen Share-Dialog oder Clipboard als Fallback
/// </summary>
[Service(UnoService.Lifetime, TryAdd = UnoService.TryAdd)]
public class ShareService : IShareService
{
    private readonly ILogger<ShareService> _logger;
    private TaskCompletionSource<ShareResult>? _shareCompletionSource;
    private string? _pendingTitle;
    private string? _pendingText;
    private Uri? _pendingUri;

    public ShareService(ILogger<ShareService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<ShareResult> ShareTextAsync(string title, string text)
    {
        return ShareAsync(title, text, null);
    }

    /// <inheritdoc />
    public Task<ShareResult> ShareLinkAsync(string title, Uri uri, string? description = null)
    {
        return ShareAsync(title, description, uri);
    }

    private Task<ShareResult> ShareAsync(string title, string? text, Uri? uri)
    {
        try
        {
            _logger.LogInformation("Sharing: {Title}, HasText: {HasText}, HasUri: {HasUri}",
                title, !string.IsNullOrEmpty(text), uri != null);

            // Check if native sharing is supported
            if (DataTransferManager.IsSupported())
            {
                _logger.LogInformation("DataTransferManager is supported, using native share dialog");
                return ShowNativeShareDialogAsync(title, text, uri);
            }
            else
            {
                _logger.LogInformation("DataTransferManager not supported, falling back to clipboard");
                return CopyToClipboardAsync(title, text, uri);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to share content");
            return Task.FromResult(ShareResult.Failed);
        }
    }

    private Task<ShareResult> ShowNativeShareDialogAsync(string title, string? text, Uri? uri)
    {
        _shareCompletionSource = new TaskCompletionSource<ShareResult>();
        _pendingTitle = title;
        _pendingText = text;
        _pendingUri = uri;

        try
        {
            var dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += OnDataRequested;

            DataTransferManager.ShowShareUI();

            // Complete immediately since we can't know when user dismisses the dialog
            _shareCompletionSource.TrySetResult(ShareResult.SharedNatively);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show native share dialog, falling back to clipboard");

            // Fall back to clipboard
            return CopyToClipboardAsync(title, text, uri);
        }

        return _shareCompletionSource.Task;
    }

    private void OnDataRequested(DataTransferManager sender, DataRequestedEventArgs args)
    {
        try
        {
            args.Request.Data.Properties.Title = _pendingTitle ?? "Teilen";

            if (!string.IsNullOrEmpty(_pendingText))
            {
                args.Request.Data.SetText(_pendingText);
            }

            if (_pendingUri != null)
            {
                args.Request.Data.SetWebLink(_pendingUri);
            }

            _logger.LogInformation("Data prepared for sharing");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DataRequested handler");
#if WINDOWS
            args.Request.FailWithDisplayText("Fehler beim Teilen");
#endif
        }
        finally
        {
            // Unregister the event handler
            sender.DataRequested -= OnDataRequested;

            // Clear pending data
            _pendingTitle = null;
            _pendingText = null;
            _pendingUri = null;
        }
    }

    private Task<ShareResult> CopyToClipboardAsync(string title, string? text, Uri? uri)
    {
        try
        {
            var dataPackage = new DataPackage();

            // Build the text to copy
            var contentParts = new List<string>();

            if (!string.IsNullOrEmpty(text))
            {
                contentParts.Add(text);
            }

            if (uri != null)
            {
                contentParts.Add(uri.ToString());
            }

            var content = string.Join("\n\n", contentParts);
            dataPackage.SetText(content);

            if (uri != null)
            {
                dataPackage.SetWebLink(uri);
            }

            Clipboard.SetContent(dataPackage);

            _logger.LogInformation("Content copied to clipboard successfully");
            return Task.FromResult(ShareResult.CopiedToClipboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy content to clipboard");
            return Task.FromResult(ShareResult.Failed);
        }
    }
}
