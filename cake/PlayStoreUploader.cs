using Cake.Common.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace Build;

/// <summary>
/// Gemeinsame Upload-Logik fuer den Play-Store-Eintrag auf Basis des
/// fastlane-kompatiblen Metadata-Layouts (cake/fastlane/metadata/android/&lt;locale&gt;/...).
/// Fehlt eine Datei in einer Locale, wird auf die Default-Locale zurueckgefallen
/// (z. B. deutsche Screenshots fuer en-US, solange die App nicht lokalisiert ist).
/// </summary>
public static class PlayStoreUploader
{
    /// <summary>Bildtypen im Play-Eintrag; Ordner-/Dateinamen identisch zu fastlane supply.</summary>
    private static readonly string[] SingleImageTypes = ["icon", "featureGraphic"];
    private static readonly string[] ScreenshotImageTypes = ["phoneScreenshots", "sevenInchScreenshots", "tenInchScreenshots"];

    private const int MaxScreenshotsPerType = 8;

    public static string MetadataRoot(BuildContext context) =>
        Path.Combine(context.FastlaneDirectory, "metadata", "android");

    public static void UploadListings(
        BuildContext context, PlayStoreClient client, string editId,
        IReadOnlyList<string> locales, string defaultLocale)
    {
        foreach (var locale in locales)
        {
            var title = ReadText(context, locale, defaultLocale, "title.txt")
                ?? throw new InvalidOperationException($"title.txt missing for {locale} (and default {defaultLocale})");
            var shortDescription = ReadText(context, locale, defaultLocale, "short_description.txt")
                ?? throw new InvalidOperationException($"short_description.txt missing for {locale}");
            var fullDescription = ReadText(context, locale, defaultLocale, "full_description.txt")
                ?? throw new InvalidOperationException($"full_description.txt missing for {locale}");
            var video = ReadText(context, locale, defaultLocale, "video.txt");

            context.Information($"[{locale}] updating listing (title: '{title}')...");
            client.UpdateListing(editId, locale, title, shortDescription, fullDescription, video);
        }
    }

    public static void UploadImages(
        BuildContext context, PlayStoreClient client, string editId,
        IReadOnlyList<string> locales, string defaultLocale)
    {
        foreach (var locale in locales)
        {
            foreach (var imageType in SingleImageTypes)
            {
                var file = FindLocaleFile(context, locale, defaultLocale, Path.Combine("images", $"{imageType}.png"))
                    ?? FindLocaleFile(context, locale, defaultLocale, Path.Combine("images", $"{imageType}.jpg"));
                if (file == null)
                {
                    context.Debug($"[{locale}] no {imageType} - skipping");
                    continue;
                }

                context.Information($"[{locale}] uploading {imageType} ({Path.GetFileName(file)})...");
                client.DeleteAllImages(editId, locale, imageType);
                client.UploadImage(editId, locale, imageType, file);
            }

            foreach (var imageType in ScreenshotImageTypes)
            {
                var dir = FindLocaleDirectory(context, locale, defaultLocale, Path.Combine("images", imageType));
                if (dir == null)
                    continue;

                var files = Directory.GetFiles(dir)
                    .Where(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                        || f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                        || f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (files.Count == 0)
                    continue;

                if (files.Count > MaxScreenshotsPerType)
                {
                    context.Warning($"[{locale}] {imageType}: {files.Count} images, Play allows {MaxScreenshotsPerType} - uploading the first {MaxScreenshotsPerType}.");
                    files = files.Take(MaxScreenshotsPerType).ToList();
                }

                context.Information($"[{locale}] uploading {files.Count} {imageType}...");
                client.DeleteAllImages(editId, locale, imageType);
                foreach (var file in files)
                {
                    client.UploadImage(editId, locale, imageType, file);
                }
            }
        }
    }

    public static void UploadAppDetails(BuildContext context, PlayStoreClient client, string editId, string defaultLocale)
    {
        var section = context.Configuration.GetSection("Android:Release");
        client.UpdateAppDetails(
            editId,
            section["ContactEmail"],
            section["ContactPhone"],
            section["ContactWebsite"],
            defaultLocale);
    }

    /// <summary>Release-Notes pro Locale: changelogs/&lt;versionCode&gt;.txt, sonst default.txt.</summary>
    public static IReadOnlyDictionary<string, string> ReadReleaseNotes(
        BuildContext context, IReadOnlyList<string> locales, string defaultLocale, int versionCode)
    {
        var notes = new Dictionary<string, string>();
        foreach (var locale in locales)
        {
            var text = ReadText(context, locale, defaultLocale, Path.Combine("changelogs", $"{versionCode}.txt"))
                ?? ReadText(context, locale, defaultLocale, Path.Combine("changelogs", "default.txt"));
            if (text != null)
            {
                notes[locale] = text;
            }
            else
            {
                context.Warning($"[{locale}] no release notes found for versionCode {versionCode}");
            }
        }
        return notes;
    }

    private static string? ReadText(BuildContext context, string locale, string defaultLocale, string relativePath)
    {
        var file = FindLocaleFile(context, locale, defaultLocale, relativePath);
        if (file == null)
            return null;
        var content = File.ReadAllText(file).Trim();
        return content.Length == 0 ? null : content;
    }

    private static string? FindLocaleFile(BuildContext context, string locale, string defaultLocale, string relativePath)
    {
        var root = MetadataRoot(context);
        var candidates = new[]
        {
            Path.Combine(root, locale, relativePath),
            Path.Combine(root, defaultLocale, relativePath)
        };
        return candidates.FirstOrDefault(File.Exists);
    }

    private static string? FindLocaleDirectory(BuildContext context, string locale, string defaultLocale, string relativePath)
    {
        var root = MetadataRoot(context);
        var candidates = new[]
        {
            Path.Combine(root, locale, relativePath),
            Path.Combine(root, defaultLocale, relativePath)
        };
        return candidates.FirstOrDefault(d => Directory.Exists(d) && Directory.GetFiles(d).Length > 0);
    }
}
