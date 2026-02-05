namespace Heimatplatz.Features.AppUpdate.Contracts.Models;

/// <summary>
/// Progress information for a flexible update download.
/// </summary>
/// <param name="BytesDownloaded">Number of bytes downloaded so far.</param>
/// <param name="TotalBytesToDownload">Total number of bytes to download.</param>
public record UpdateDownloadProgress(long BytesDownloaded, long TotalBytesToDownload)
{
    /// <summary>
    /// Gets the download progress as a percentage (0-100).
    /// </summary>
    public double PercentComplete => TotalBytesToDownload > 0
        ? (double)BytesDownloaded / TotalBytesToDownload * 100
        : 0;
}
