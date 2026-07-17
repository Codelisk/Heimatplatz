using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Features.Properties.Models;
using Microsoft.Extensions.Logging;
using MauiPermissions = Microsoft.Maui.ApplicationModel.Permissions;

namespace Heimatplatz.Maui.Features.Properties.Presentation.Wizard;

/// <summary>
/// Schritt 1: Fotos. Der Upload laeuft eager im Hintergrund (einzeln pro Datei,
/// nur Items ohne RemoteUrl). Videos bietet der Wizard nicht mehr an - Items aus
/// alten Entwuerfen werden nur noch angezeigt und beim Loeschen mit aufgeraeumt.
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
            var photoText = photos == 1 ? "1 Foto" : $"{photos} Fotos";
            var videoText = videos == 1 ? "1 Video" : $"{videos} Videos";
            return videos > 0 ? $"{photoText} · {videoText}" : photoText;
        }
    }

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
                ErrorMessage = "Kamera-Berechtigung wurde nicht erteilt.";
                return;
            }

            var file = await MediaPicker.Default.CapturePhotoAsync();
            if (file == null)
                return;

            await AddMediaFileAsync(file);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PropertyWizard] Fehler bei Foto-Aufnahme");
            ErrorMessage = $"Fehler bei der Aufnahme: {ex.Message}";
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
                Title = "Fotos auswählen"
            });
            if (files == null)
                return;

            foreach (var file in files)
            {
                if (Media.Count(m => m.IsPhoto) >= MaxPhotos)
                {
                    ErrorMessage = $"Maximal {MaxPhotos} Fotos erlaubt";
                    break;
                }

                await AddMediaFileAsync(file);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PropertyWizard] Fehler bei Foto-Auswahl");
            ErrorMessage = $"Fehler bei der Auswahl: {ex.Message}";
        }
    }

    [RelayCommand]
    private void RemoveMedia(WizardMediaItem item)
    {
        // Bereits hochgeladene Datei bleibt bis zur Entwurfs-Loeschung am Server liegen -
        // sie faellt aber aus den URL-Listen und landet damit nie im Inserat.
        Media.Remove(item);
        MediaCount = Media.Count;
        OnPropertyChanged(nameof(MediaCountText));
        MarkDraftDirty();
    }

    private bool EnsurePhotoCapacity()
    {
        if (Media.Count(m => m.IsPhoto) >= MaxPhotos)
        {
            ErrorMessage = $"Maximal {MaxPhotos} Fotos erlaubt";
            return false;
        }
        return true;
    }

    private async Task AddMediaFileAsync(FileResult file)
    {
        using var stream = await file.OpenReadAsync();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        var bytes = memoryStream.ToArray();

        var contentType = string.IsNullOrEmpty(file.ContentType)
            ? GuessContentType(file.FileName)
            : file.ContentType;

        Media.Add(WizardMediaItem.FromPicked(file.FileName, contentType, bytes, isVideo: false));
        MediaCount = Media.Count;
        OnPropertyChanged(nameof(MediaCountText));
        ErrorMessage = null;
        MarkDraftDirty();
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
        if (Media.Any(m => !m.IsUploaded && m.Data != null))
        {
            _uploadTask = UploadPendingMediaAsync();
            await _uploadTask;
        }
    }

    private async Task UploadPendingMediaAsync()
    {
        foreach (var item in Media.Where(m => !m.IsUploaded && m.Data != null).ToList())
        {
            try
            {
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
                                    Base64Data = item.ToBase64()
                                }
                            ]
                        }
                    });

                item.RemoteUrl = item.IsVideo
                    ? uploadResult?.VideoUrls?.FirstOrDefault()
                    : uploadResult?.ImageUrls?.FirstOrDefault();

                if (item.IsUploaded)
                    MarkDraftDirty();
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
