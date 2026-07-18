using Cake.Common.Diagnostics;
using Cake.Frosting;
using Microsoft.Extensions.Configuration;

namespace Build.Tasks;

/// <summary>
/// Android-Gegenstueck zu UpdateMetadataIos, aber ohne fastlane: laedt Store-Texte
/// (title/short/full/video), Bilder (Icon, Feature-Graphic) und Screenshots fuer alle
/// konfigurierten Locales direkt ueber die Play Developer API hoch - KEIN Binary,
/// KEINE Track-Aenderung. Laeuft auf Windows/Linux/macOS (nur .NET + Service-Account-Key).
/// Konfiguration: Sektion "Android:Release" (Locales, DefaultLocale, Kontaktdaten).
/// </summary>
[TaskName("UpdateMetadataAndroid")]
public sealed class UpdateMetadataAndroidTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Information("=== Update Android Store Metadata (Play Developer API) ===");

        if (string.IsNullOrEmpty(context.PlayStoreJsonKeyPath) || !File.Exists(context.PlayStoreJsonKeyPath))
        {
            throw new InvalidOperationException(
                $"Play Store JSON key not found: '{context.PlayStoreJsonKeyPath}'. " +
                "Configure Android:PlayStoreJsonKeyPath or PLAY_STORE_JSON_KEY_PATH.");
        }

        var locales = AndroidStoreTextsTask.GetLocales(context);
        var defaultLocale = context.Configuration["Android:Release:DefaultLocale"] ?? locales[0];

        // Icon + Feature-Grafik frisch aus den Markenassets rendern (Drift-Schutz)
        StoreArtTask.RunCore(context);

        using var client = new PlayStoreClient(context.PlayStoreJsonKeyPath, context.AndroidPackageName);
        var editId = client.CreateEdit();
        context.Information($"Edit created: {editId}");

        PlayStoreUploader.UploadListings(context, client, editId, locales, defaultLocale);
        PlayStoreUploader.UploadImages(context, client, editId, locales, defaultLocale);
        PlayStoreUploader.UploadAppDetails(context, client, editId, defaultLocale);

        client.CommitEdit(editId);
        context.Information("Android store metadata uploaded and committed successfully!");
    }
}
