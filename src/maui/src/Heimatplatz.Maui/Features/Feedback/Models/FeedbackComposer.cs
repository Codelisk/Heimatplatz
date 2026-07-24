using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Core.Media;
using Heimatplatz.Maui.Features.Feedback.Services;
using Heimatplatz.Maui.Localization.Feedback;
using Microsoft.Extensions.Logging;
using Shiny;
using MauiPermissions = Microsoft.Maui.ApplicationModel.Permissions;

namespace Heimatplatz.Maui.Features.Feedback.Models;

/// <summary>
/// Nachrichten-Eingabe im Messenger-Stil: Text, Bild-Anhaenge und Sprachnachricht
/// haengen direkt an der Eingabezeile (Control <c>MessageComposer</c>). Wird von der
/// Compose-Seite (neue Anfrage) und der Antwortzeile im Verlauf gemeinsam genutzt -
/// dadurch koennen auch Antworten Bilder und Sprachnachrichten enthalten.
/// </summary>
[Transient]
public partial class FeedbackComposer(
    IVoiceRecorderService voiceRecorder,
    IAudioPlaybackService playback,
    IPhotoPreviewService photoPreview,
    IFeedbackService feedbackService,
    ILogger<FeedbackComposer> logger,
    FeedbackStringsLocalized loc
) : ObservableObject
{
    private const int MaxPhotos = 5;
    private const string MediaCacheSubfolder = "feedback-media";

    private static readonly Dictionary<string, string> ExtensionToContentType = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp"
    };

    private bool _playbackHooked;

    public FeedbackStringsLocalized Loc { get; } = loc;

    public ObservableCollection<FeedbackPhotoItem> Photos { get; } = [];

    /// <summary>Sprachaufnahme ist nur auf Android/iOS verfuegbar.</summary>
    public bool IsVoiceSupported => voiceRecorder.IsSupported;

    /// <summary>
    /// Kamera-Aktion nur anzeigen, wenn der Plattformhandler sie zuverlässig
    /// unterstützt. Der Windows-MediaPicker meldet Capture-Support, obwohl er
    /// in der Desktop-App keine sichtbare Aufnahmeaktion startet.
    /// </summary>
    public bool IsCameraSupported =>
        !OperatingSystem.IsWindows() && MediaPicker.Default.IsCaptureSupported;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSend))]
    [NotifyPropertyChangedFor(nameof(ShowMicButton))]
    [NotifyPropertyChangedFor(nameof(ShowSendButton))]
    public partial string Text { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    public partial bool IsRecording { get; set; }

    [ObservableProperty]
    public partial string RecordingText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRecording))]
    [NotifyPropertyChangedFor(nameof(HasAttachments))]
    [NotifyPropertyChangedFor(nameof(CanSend))]
    [NotifyPropertyChangedFor(nameof(ShowMicButton))]
    [NotifyPropertyChangedFor(nameof(ShowSendButton))]
    public partial VoiceRecording? Recording { get; set; }

    [ObservableProperty]
    public partial string VoiceDurationText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsPreviewPlaying { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    public partial bool IsSending { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; set; }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public bool HasRecording => Recording != null;

    public bool HasPhotos => Photos.Count > 0;

    public bool HasAttachments => HasPhotos || HasRecording;

    /// <summary>Normale Eingabezeile (weder Aufnahme noch Versand laeuft).</summary>
    public bool IsIdle => !IsRecording && !IsSending;

    /// <summary>Wie im Messenger: leeres Feld ohne Anhaenge zeigt das Mikrofon, sonst den Senden-Pfeil.</summary>
    public bool ShowMicButton => IsVoiceSupported && string.IsNullOrWhiteSpace(Text) && !HasAttachments;

    public bool ShowSendButton => !ShowMicButton;

    public bool CanSend => !string.IsNullOrWhiteSpace(Text) || HasAttachments;

    public FeedbackComposer Initialize()
    {
        if (_playbackHooked)
            return this;

        _playbackHooked = true;
        playback.PlaybackEnded += (_, _) =>
            MainThread.BeginInvokeOnMainThread(() => IsPreviewPlaying = false);
        Photos.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasPhotos));
            OnPropertyChanged(nameof(HasAttachments));
            OnPropertyChanged(nameof(CanSend));
            OnPropertyChanged(nameof(ShowMicButton));
            OnPropertyChanged(nameof(ShowSendButton));
        };
        return this;
    }

    #region Bilder

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
                if (Photos.Count >= MaxPhotos)
                {
                    ErrorMessage = Loc.MaxPhotosFormat(MaxPhotos);
                    break;
                }

                await AddPhotoFileAsync(file);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Feedback] Fehler bei Foto-Auswahl");
            ErrorMessage = Loc.SendFailedFormat(ex.Message);
        }
    }

    [RelayCommand]
    private async Task TakePhotoAsync()
    {
        try
        {
            if (!EnsurePhotoCapacity())
                return;

            if (!IsCameraSupported)
            {
                ErrorMessage = Loc.CameraUnavailable;
                return;
            }

            var cameraStatus = await MauiPermissions.RequestAsync<MauiPermissions.Camera>();
            if (cameraStatus != PermissionStatus.Granted)
            {
                ErrorMessage = Loc.CameraPermissionDenied;
                return;
            }

            var file = await MediaPicker.Default.CapturePhotoAsync();
            if (file == null)
                return;

            await AddPhotoFileAsync(file);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Feedback] Fehler bei Foto-Aufnahme");
            ErrorMessage = Loc.SendFailedFormat(ex.Message);
        }
    }

    [RelayCommand]
    private void RemovePhoto(FeedbackPhotoItem item)
    {
        Photos.Remove(item);
        MediaFileCache.TryDelete(item.FilePath);
    }

    private bool EnsurePhotoCapacity()
    {
        if (Photos.Count < MaxPhotos)
        {
            ErrorMessage = null;
            return true;
        }

        ErrorMessage = Loc.MaxPhotosFormat(MaxPhotos);
        return false;
    }

    private async Task AddPhotoFileAsync(FileResult file)
    {
        // Original 1:1 in den Cache (Upload liest spaeter von Platte), Vorschau klein rendern
        var cachedPath = await MediaFileCache.CopyAsync(file, MediaCacheSubfolder);
        var previewBytes = await photoPreview.CreatePreviewAsync(cachedPath, maxDimension: 800);

        Photos.Add(new FeedbackPhotoItem
        {
            FilePath = cachedPath,
            FileName = file.FileName,
            ContentType = ExtensionToContentType.GetValueOrDefault(Path.GetExtension(file.FileName), "image/jpeg"),
            Preview = ImageSource.FromStream(() => new MemoryStream(previewBytes))
        });
    }

    #endregion

    #region Sprachnachricht

    [RelayCommand]
    private async Task StartRecordingAsync()
    {
        try
        {
            if (!await voiceRecorder.RequestAccessAsync())
            {
                ErrorMessage = Loc.MicPermissionDenied;
                return;
            }

            DeleteRecording();
            ErrorMessage = null;
            RecordingText = "0:00";

            await voiceRecorder.StartAsync();
            IsRecording = true;
            _ = RunRecordingTickerAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Feedback] Aufnahme-Start fehlgeschlagen");
            IsRecording = false;
            ErrorMessage = Loc.RecordFailed;
        }
    }

    /// <summary>Aufnahme beenden und als Anhang uebernehmen.</summary>
    [RelayCommand]
    private async Task StopRecordingAsync()
    {
        try
        {
            var recording = await voiceRecorder.StopAsync();
            IsRecording = false;

            if (recording == null)
            {
                ErrorMessage = Loc.RecordFailed;
                return;
            }

            Recording = recording;
            VoiceDurationText = FeedbackDisplay.FormatDuration(recording.DurationSeconds);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Feedback] Aufnahme-Stopp fehlgeschlagen");
            IsRecording = false;
            ErrorMessage = Loc.RecordFailed;
        }
    }

    /// <summary>Aufnahme abbrechen und verwerfen (Papierkorb waehrend der Aufnahme).</summary>
    [RelayCommand]
    private async Task CancelRecordingAsync()
    {
        try
        {
            await voiceRecorder.CancelAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Feedback] Aufnahme-Abbruch fehlgeschlagen");
        }
        finally
        {
            IsRecording = false;
            RecordingText = string.Empty;
        }
    }

    [RelayCommand]
    private void DeleteRecording()
    {
        if (playback.IsPlaying)
            _ = playback.StopAsync();
        IsPreviewPlaying = false;

        if (Recording != null)
        {
            MediaFileCache.TryDelete(Recording.FilePath);
            Recording = null;
        }

        VoiceDurationText = string.Empty;
    }

    [RelayCommand]
    private async Task TogglePreviewPlaybackAsync()
    {
        if (Recording == null)
            return;

        if (IsPreviewPlaying)
        {
            await playback.StopAsync();
            IsPreviewPlaying = false;
            return;
        }

        IsPreviewPlaying = await playback.PlayFileAsync(Recording.FilePath);
        if (!IsPreviewPlaying)
            ErrorMessage = Loc.AudioPlayFailed;
    }

    private async Task RunRecordingTickerAsync()
    {
        while (voiceRecorder.IsRecording)
        {
            RecordingText = FeedbackDisplay.FormatDuration(voiceRecorder.Elapsed.TotalSeconds);
            await Task.Delay(500);
        }
    }

    #endregion

    /// <summary>
    /// Laedt alle Anhaenge einzeln hoch (grosse Originale) und liefert die Referenzen
    /// fuer Create/AddMessage. <paramref name="onProgress"/> meldet (aktuell, gesamt).
    /// </summary>
    public async Task<List<FeedbackAttachmentInput>> UploadAttachmentsAsync(
        Action<int, int>? onProgress = null,
        CancellationToken ct = default)
    {
        var attachments = new List<FeedbackAttachmentInput>();
        var total = Photos.Count + (Recording != null ? 1 : 0);
        var current = 0;

        foreach (var photo in Photos)
        {
            onProgress?.Invoke(++current, total);
            var bytes = await File.ReadAllBytesAsync(photo.FilePath, ct);
            var uploaded = await feedbackService.UploadAttachmentAsync(photo.FileName, photo.ContentType, bytes, ct);
            attachments.Add(new FeedbackAttachmentInput { Url = uploaded.Url });
        }

        if (Recording != null)
        {
            onProgress?.Invoke(++current, total);
            var bytes = await File.ReadAllBytesAsync(Recording.FilePath, ct);
            var uploaded = await feedbackService.UploadAttachmentAsync("sprachnachricht.wav", "audio/wav", bytes, ct);
            attachments.Add(new FeedbackAttachmentInput
            {
                Url = uploaded.Url,
                DurationSeconds = Recording.DurationSeconds
            });
        }

        return attachments;
    }

    /// <summary>Beendet laufende Aufnahme/Wiedergabe (beim Verlassen der Seite).</summary>
    public void Suspend()
    {
        if (voiceRecorder.IsRecording)
            _ = voiceRecorder.CancelAsync();
        if (playback.IsPlaying)
            _ = playback.StopAsync();
        IsRecording = false;
        IsPreviewPlaying = false;
    }

    /// <summary>Leert Eingabe und Anhaenge und raeumt die lokalen Dateien weg.</summary>
    public void Reset()
    {
        Text = string.Empty;
        ErrorMessage = null;

        foreach (var photo in Photos)
            MediaFileCache.TryDelete(photo.FilePath);
        Photos.Clear();

        DeleteRecording();
    }

    /// <summary>Sorgt dafuer, dass eine noch laufende Aufnahme vor dem Senden uebernommen wird.</summary>
    public async Task FinishPendingRecordingAsync()
    {
        if (voiceRecorder.IsRecording)
            await StopRecordingAsync();
        if (playback.IsPlaying)
            await playback.StopAsync();
    }
}
