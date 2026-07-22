using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Core.Media;
using Heimatplatz.Maui.Features.Feedback.Models;
using Heimatplatz.Maui.Features.Feedback.Services;
using Heimatplatz.Maui.Localization.Feedback;
using Microsoft.Extensions.Logging;
using Shiny;
using MauiPermissions = Microsoft.Maui.ApplicationModel.Permissions;

namespace Heimatplatz.Maui.Features.Feedback.Presentation;

/// <summary>
/// Neues Feedback: Kategorie-Chips, Text, Fotos (Original in den Cache, kleine
/// Vorschau rendern - Wizard-Muster) und Sprachnachricht (Android/iOS). Anhaenge
/// werden beim Senden einzeln hochgeladen, danach wird das Ticket erstellt.
/// </summary>
[ShellMap<FeedbackComposePage>("FeedbackCompose")]
public partial class FeedbackComposeViewModel : ObservableObject, IPageLifecycleAware
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

    private readonly IFeedbackService _feedbackService;
    private readonly IVoiceRecorderService _voiceRecorder;
    private readonly IAudioPlaybackService _playback;
    private readonly IPhotoPreviewService _photoPreview;
    private readonly INavigator _navigator;
    private readonly ILogger<FeedbackComposeViewModel> _logger;

    public FeedbackComposeViewModel(
        IFeedbackService feedbackService,
        IVoiceRecorderService voiceRecorder,
        IAudioPlaybackService playback,
        IPhotoPreviewService photoPreview,
        INavigator navigator,
        ILogger<FeedbackComposeViewModel> logger,
        FeedbackStringsLocalized loc)
    {
        _feedbackService = feedbackService;
        _voiceRecorder = voiceRecorder;
        _playback = playback;
        _photoPreview = photoPreview;
        _navigator = navigator;
        _logger = logger;
        Loc = loc;

        Categories =
        [
            new FeedbackCategoryOption { Value = FeedbackCategory.Idea, Label = loc.CategoryIdea, IsSelected = true },
            new FeedbackCategoryOption { Value = FeedbackCategory.Problem, Label = loc.CategoryProblem },
            new FeedbackCategoryOption { Value = FeedbackCategory.Question, Label = loc.CategoryQuestion },
            new FeedbackCategoryOption { Value = FeedbackCategory.Praise, Label = loc.CategoryPraise },
            new FeedbackCategoryOption { Value = FeedbackCategory.Other, Label = loc.CategoryOther }
        ];

        _playback.PlaybackEnded += (_, _) =>
            MainThread.BeginInvokeOnMainThread(() => IsPreviewPlaying = false);
    }

    public FeedbackStringsLocalized Loc { get; }

    public ObservableCollection<FeedbackCategoryOption> Categories { get; }

    public ObservableCollection<FeedbackPhotoItem> Photos { get; } = [];

    public bool IsVoiceSupported => _voiceRecorder.IsSupported;

    [ObservableProperty]
    public partial string Subject { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Body { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    public partial bool IsBusy { get; set; }

    public bool IsNotBusy => !IsBusy;

    [ObservableProperty]
    public partial string? BusyMessage { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; set; }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    [ObservableProperty]
    public partial bool IsRecording { get; set; }

    [ObservableProperty]
    public partial string RecordingText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRecording))]
    [NotifyPropertyChangedFor(nameof(ShowRecordButton))]
    public partial VoiceRecording? Recording { get; set; }

    public bool HasRecording => Recording != null;

    /// <summary>Aufnehmen-Button nur zeigen, solange weder aufgenommen wird noch eine Aufnahme existiert.</summary>
    public bool ShowRecordButton => !IsRecording && Recording == null;

    [ObservableProperty]
    public partial string VoiceDurationText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsPreviewPlaying { get; set; }

    public void OnAppearing()
    {
    }

    public void OnDisappearing()
    {
        // Laufende Aufnahme/Wiedergabe nicht weiterlaufen lassen, wenn die Seite verlassen wird
        if (_voiceRecorder.IsRecording)
            _ = _voiceRecorder.CancelAsync();
        if (_playback.IsPlaying)
            _ = _playback.StopAsync();
    }

    partial void OnIsRecordingChanged(bool value) => OnPropertyChanged(nameof(ShowRecordButton));

    [RelayCommand]
    private void SelectCategory(FeedbackCategoryOption option)
    {
        foreach (var category in Categories)
            category.IsSelected = category == option;
    }

    #region Fotos

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
            _logger.LogError(ex, "[Feedback] Fehler bei Foto-Auswahl");
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
            _logger.LogError(ex, "[Feedback] Fehler bei Foto-Aufnahme");
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
        var previewBytes = await _photoPreview.CreatePreviewAsync(cachedPath, maxDimension: 800);

        var extension = Path.GetExtension(file.FileName);
        Photos.Add(new FeedbackPhotoItem
        {
            FilePath = cachedPath,
            FileName = file.FileName,
            ContentType = ExtensionToContentType.GetValueOrDefault(extension, "image/jpeg"),
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
            if (!await _voiceRecorder.RequestAccessAsync())
            {
                ErrorMessage = Loc.MicPermissionDenied;
                return;
            }

            DeleteRecording();
            ErrorMessage = null;
            RecordingText = Loc.RecordingFormat("0:00");

            await _voiceRecorder.StartAsync();
            IsRecording = true;
            _ = RunRecordingTickerAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Feedback] Aufnahme-Start fehlgeschlagen");
            IsRecording = false;
            ErrorMessage = Loc.RecordFailed;
        }
    }

    [RelayCommand]
    private async Task StopRecordingAsync()
    {
        try
        {
            var recording = await _voiceRecorder.StopAsync();
            IsRecording = false;

            if (recording == null)
            {
                ErrorMessage = Loc.RecordFailed;
                return;
            }

            Recording = recording;
            VoiceDurationText = Loc.VoiceDurationFormat(FeedbackDisplay.FormatDuration(recording.DurationSeconds));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Feedback] Aufnahme-Stopp fehlgeschlagen");
            IsRecording = false;
            ErrorMessage = Loc.RecordFailed;
        }
    }

    [RelayCommand]
    private void DeleteRecording()
    {
        if (_playback.IsPlaying)
            _ = _playback.StopAsync();
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
            await _playback.StopAsync();
            IsPreviewPlaying = false;
            return;
        }

        IsPreviewPlaying = await _playback.PlayFileAsync(Recording.FilePath);
        if (!IsPreviewPlaying)
            ErrorMessage = Loc.AudioPlayFailed;
    }

    /// <summary>Aktualisiert die Aufnahme-Anzeige, solange aufgenommen wird (Dauer aus den PCM-Bytes).</summary>
    private async Task RunRecordingTickerAsync()
    {
        while (_voiceRecorder.IsRecording)
        {
            RecordingText = Loc.RecordingFormat(FeedbackDisplay.FormatDuration(_voiceRecorder.Elapsed.TotalSeconds));
            await Task.Delay(500);
        }
    }

    #endregion

    [RelayCommand]
    private async Task SendAsync()
    {
        var body = Body.Trim();
        if (body.Length == 0 && Photos.Count == 0 && Recording == null)
        {
            ErrorMessage = Loc.ErrorEmpty;
            return;
        }

        // Eine noch laufende Aufnahme gehoert mit dazu
        if (_voiceRecorder.IsRecording)
            await StopRecordingAsync();
        if (_playback.IsPlaying)
            await _playback.StopAsync();

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var attachments = new List<FeedbackAttachmentInput>();
            var total = Photos.Count + (Recording != null ? 1 : 0);
            var current = 0;

            // Ein Anhang pro Request (grosse Original-Fotos) - Wizard-Upload-Muster
            foreach (var photo in Photos)
            {
                BusyMessage = Loc.UploadingFormat(++current, total);
                var bytes = await File.ReadAllBytesAsync(photo.FilePath);
                var uploaded = await _feedbackService.UploadAttachmentAsync(photo.FileName, photo.ContentType, bytes);
                attachments.Add(new FeedbackAttachmentInput { Url = uploaded.Url });
            }

            if (Recording != null)
            {
                BusyMessage = Loc.UploadingFormat(++current, total);
                var bytes = await File.ReadAllBytesAsync(Recording.FilePath);
                var uploaded = await _feedbackService.UploadAttachmentAsync("sprachnachricht.wav", "audio/wav", bytes);
                attachments.Add(new FeedbackAttachmentInput
                {
                    Url = uploaded.Url,
                    DurationSeconds = Recording.DurationSeconds
                });
            }

            BusyMessage = Loc.Sending;
            var category = Categories.First(c => c.IsSelected).Value;
            var subject = string.IsNullOrWhiteSpace(Subject) ? null : Subject.Trim();
            await _feedbackService.CreateTicketAsync(category, subject, body, attachments);

            CleanupLocalFiles();
            await _navigator.GoBack();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Feedback] Senden fehlgeschlagen");
            ErrorMessage = Loc.SendFailedFormat(ex.Message);
        }
        finally
        {
            IsBusy = false;
            BusyMessage = null;
        }
    }

    private void CleanupLocalFiles()
    {
        foreach (var photo in Photos)
            MediaFileCache.TryDelete(photo.FilePath);
        Photos.Clear();

        if (Recording != null)
        {
            MediaFileCache.TryDelete(Recording.FilePath);
            Recording = null;
        }
    }
}
