namespace Heimatplatz.Maui.Core.Media;

/// <summary>
/// Legt gepickte Original-Fotos 1:1 in einem eigenen Cache-Ordner ab. Die
/// MediaPicker-Tempdatei kann das OS jederzeit wegraeumen; der Upload liest
/// spaeter von dieser Kopie statt alle Fotos im RAM zu halten.
/// </summary>
public static class MediaFileCache
{
    public static async Task<string> CopyAsync(FileResult file, string subfolder)
    {
        var cacheDir = Path.Combine(FileSystem.CacheDirectory, subfolder);
        Directory.CreateDirectory(cacheDir);

        var extension = Path.GetExtension(file.FileName);
        var targetPath = Path.Combine(cacheDir, $"{Guid.NewGuid():N}{extension}");

        await using var source = await file.OpenReadAsync();
        await using var target = File.Create(targetPath);
        await source.CopyToAsync(target);

        return targetPath;
    }

    public static void TryDelete(string? path)
    {
        try
        {
            if (path != null && File.Exists(path))
                File.Delete(path);
        }
        catch (IOException)
        {
            // Cache-Datei raeumt sonst das OS auf
        }
    }
}
