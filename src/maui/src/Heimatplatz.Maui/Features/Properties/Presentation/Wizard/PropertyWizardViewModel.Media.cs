using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Core.Media;
using Heimatplatz.Maui.Features.Properties.Models;
using Microsoft.Extensions.Logging;
using MauiPermissions = Microsoft.Maui.ApplicationModel.Permissions;

namespace Heimatplatz.Maui.Features.Properties.Presentation.Wizard;

/// <summary>
/// Fotos des Editors. Der Upload laeuft eager im Hintergrund (einzeln pro Datei,
/// nur Items ohne RemoteUrl). Videos bietet der Editor nicht mehr an - Items aus
/// alten Entwuerfen werden nur noch angezeigt und beim Loeschen mit aufgeraeumt.
/// Das Hero-Foto oben zeigt wie die Detailansicht jeweils EIN Foto samt
/// ‹/›-Navigation; das erste Foto ist das Titelbild.
/// </summary>
public partial class PropertyWizardViewModel
{
    private const int MaxPhotos = 20;

    private Task? _uploadTask;

    public ObservableCollection<WizardMediaItem> Media { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMedia))]
    [NotifyPropertyChangedFor(nameof(HasNoMedia))]
    [NotifyPropertyChangedFor(nameof(MediaCountText))]
    public partial int MediaCount { get; set; }

    public bool HasMedia => MediaCount > 0;
    public bool HasNoMedia => MediaCount == 0;

    public string MediaCountText
    {
        get
        {
            var photos = Media.Count(m => m.IsPhoto);
            var videos = Media.Count(m => m.IsVideo);
            var photoText = photos == 1 ? Loc.PhotoSingular : Loc.PhotosCountFormat(photos);
            var videoText = videos == 1 ? Loc.VideoSingular : Loc.VideosCountFormat(videos);
            return videos > 0 ? $"{photoText} · {videoText}" : photoText;
        }
    }

    #region Hero-Foto (Einzelbild + ‹/›-Navigation wie die Detailansicht)

    private List<WizardMediaItem> PhotoItems => Media.Where(m => m.IsPhoto).ToList();

    [ObservableProperty]
    public partial int HeroIndex { get; set; }

    public ImageSource? CurrentHeroSource
    {
        get
        {
            var photos = PhotoItems;
            return photos.Count == 0 ? null : photos[Math.Clamp(HeroIndex, 0, photos.Count - 1)].DisplaySource;
        }
    }

    public string HeroCounterText
    {
        get
        {
            var count = PhotoItems.Count;
            return count == 0 ? string.Empty : $"{Math.Clamp(HeroIndex, 0, count - 1) + 1} / {count}";
        }
    }

    public bool HasPhotos => Media.Any(m => m.IsPhoto);
    public bool HasMultiplePhotos => Media.Count(m => m.IsPhoto) > 1;

    [RelayCommand]
    private void ShowPreviousHeroImage()
    {
        var count = PhotoItems.Count;
        if (count < 2) return;
        HeroIndex = (HeroIndex - 1 + count) % count;
        RefreshHero();
    }

    [RelayCommand]
    private void ShowNextHeroImage()
    {
        var count = PhotoItems.Count;
        if (count < 2) return;
        HeroIndex = (HeroIndex + 1) % count;
        RefreshHero();
    }

    private void RefreshHero()
    {
        var count = PhotoItems.Count;
        if (HeroIndex >= count)
            HeroIndex = Math.Max(0, count - 1);

        OnPropertyChanged(nameof(CurrentHeroSource));
        OnPropertyChanged(nameof(HeroCounterText));
        OnPropertyChanged(nameof(HasPhotos));
        OnPropertyChanged(nameof(HasMultiplePhotos));
    }

    #endregion

    private List<string> UploadedImageUrls =>
        Media.Where(m => m.IsPhoto && m.IsUploaded).Select(m => m.RemoteUrl!).ToList();

    private List<string> UploadedVideoUrls =>
        Media.Where(m => m.IsVideo && m.IsUploaded).Select(m => m.RemoteUrl!).ToList();

    #region Picker-Commands

    [RelayCommand]
    private async Task TakePhotoAsync()
    {
        try
        {
            if (!EnsurePhotoCapacity())
                return;

            var cameraStatus = await MauiPermissions.RequestAsync<MauiPermissions.Camera>();
            if (cameraStatus != PermissionStatus.Granted)
            {
                ErrorMessage = Loc.CameraPermissionDenied;
                return;
            }

            // Bewusst ohne Resize-Optionen: das Original bleibt in voller Qualitaet
            // erhalten, verkleinert wird nur die lokale Vorschau bzw. serverseitig
            // die Anzeige-Variante
            var file = await MediaPicker.Default.CapturePhotoAsync();
            if (file == null)
                return;

            await AddMediaFileAsync(file);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PropertyWizard] Fehler bei Foto-Aufnahme");
            ErrorMessage = Loc.CaptureErrorFormat(ex.Message);
        }
    }

    [RelayCommand]
    private async Task PickPhotosAsync()
    {
        try
        {
            if (!EnsurePhotoCapacity())
                return;

            var files = await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions
            {
                Title = Loc.PickPhotosTitle
            });
            if (files == null)
                return;

            foreach (var file in files)
            {
                if (Media.Count(m => m.IsPhoto) >= MaxPhotos)
                {
                    ErrorMessage = Loc.MaxPhotosFormat(MaxPhotos);
                    break;
                }

                await AddMediaFileAsync(file);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PropertyWizard] Fehler bei Foto-Auswahl");
            ErrorMessage = Loc.PickErrorFormat(ex.Message);
        }
    }

    [RelayCommand]
    private void RemoveMedia(WizardMediaItem item)
    {
        // Bereits hochgeladene Datei bleibt bis zur Entwurfs-Loeschung am Server liegen -
        // sie faellt aber aus den URL-Listen und landet damit nie im Inserat.
        item.TryDeleteLocalFile();
        Media.Remove(item);
        MediaCount = Media.Count;
        OnPropertyChanged(nameof(MediaCountText));
        RefreshHero();
        RefreshEditorState();
        MarkDraftDirty();
        ScheduleAutoSave();
    }

    private bool EnsurePhotoCapacity()
    {
        if (Media.Count(m => m.IsPhoto) >= MaxPhotos)
        {
            ErrorMessage = Loc.MaxPhotosFormat(MaxPhotos);
            return false;
        }
        return true;
    }

    private async Task AddMediaFileAsync(FileResult file)
    {
        // Original unveraendert in den eigenen Cache kopieren (Upload liest spaeter
        // von Platte statt alle Fotos im RAM zu halten) + kleine Vorschau erzeugen
        var localPath = await MediaFileCache.CopyAsync(file, "wizard-media");
        var preview = await _photoPreview.CreatePreviewAsync(localPath);

        var contentType = string.IsNullOrEmpty(file.ContentType)
            ? GuessContentType(file.FileName)
            : file.ContentType;

        Media.Add(WizardMediaItem.FromPicked(file.FileName, contentType, localPath, preview, isVideo: false));
        MediaCount = Media.Count;
        OnPropertyChanged(nameof(MediaCountText));
        ErrorMessage = null;
        RefreshHero();
        RefreshEditorState();
        MarkDraftDirty();
        ScheduleAutoSave();

        // Ohne Schrittwechsel gibt es keinen spaeteren Trigger - Upload sofort anstossen,
        // damit die Fotos beim Veroeffentlichen laengst am Server liegen.
        // Edit-Modus laedt erst beim Speichern hoch: es gibt keinen Entwurf, der die
        // Dateien referenziert - bei Abbruch blieben sonst verwaiste Uploads am Server.
        if (!IsEditMode)
            StartMediaUpload();
    }

    private static string GuessContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".heic" => "image/heic",
            _ => "image/jpeg"
        };
    }


    #endregion

    #region Upload

    /// <summary>Startet den Hintergrund-Upload aller noch nicht hochgeladenen Medien.</summary>
    private void StartMediaUpload()
    {
        if (_uploadTask == null || _uploadTask.IsCompleted)
            _uploadTask = UploadPendingMediaAsync();
    }

    /// <summary>
    /// Wartet auf den laufenden Upload (und stoesst ausstehende Dateien erneut an) -
    /// wird vor dem Beschreibungs-Job und der Veroeffentlichung aufgerufen.
    /// </summary>
    private async Task EnsureMediaUploadedAsync()
    {
        StartMediaUpload();
        if (_uploadTask != null)
            await _uploadTask;

        // Fehlversuche vom Hintergrundlauf einmal direkt nachziehen
        if (Media.Any(m => !m.IsUploaded && m.HasLocalFile))
        {
            _uploadTask = UploadPendingMediaAsync();
            await _uploadTask;
        }
    }

    private async Task UploadPendingMediaAsync()
    {
        foreach (var item in Media.Where(m => !m.IsUploaded && m.HasLocalFile).ToList())
        {
            try
            {
                var originalBytes = await item.ReadOriginalAsync();
                var (_, uploadResult) = await _mediator.Request(
                    new UploadListingMediaHttpRequest
                    {
                        Body = new UploadListingMediaRequest
                        {
                            Media =
                            [
                                new Base64MediaData
                                {
                                    FileName = item.FileName,
                                    ContentType = item.ContentType,
                                    Base64Data = Convert.ToBase64String(originalBytes)
                                }
                            ]
                        }
                    });

                item.RemoteUrl = item.IsVideo
                    ? uploadResult?.VideoUrls?.FirstOrDefault()
                    : uploadResult?.ImageUrls?.FirstOrDefault();

                if (item.IsUploaded)
                {
                    // Original liegt jetzt am Server - Cache-Kopie wird nicht mehr gebraucht
                    item.TryDeleteLocalFile();
                    MarkDraftDirty();
                }
            }
            catch (Exception ex)
            {
                // Einzelner Fehlversuch blockiert nichts - EnsureMediaUploadedAsync
                // versucht es vor Beschreibungs-Job/Publish erneut
                _logger.LogWarning(ex, "[PropertyWizard] Upload fehlgeschlagen: {FileName}", item.FileName);
            }
        }
    }

    #endregion
}
