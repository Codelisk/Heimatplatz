using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Maui.Features.Feedback.Models;
using Heimatplatz.Maui.Features.Feedback.Services;
using Heimatplatz.Maui.Localization;
using Heimatplatz.Maui.Localization.Feedback;
using Microsoft.Extensions.Logging;
using Shiny;

namespace Heimatplatz.Maui.Features.Feedback.Presentation;

/// <summary>
/// Verlauf einer Anfrage: Ticket-Kopf (Kategorie-Icon, Status-Ampel, Umbenennen des
/// Auto-Titels) und die Korrespondenz (eigene Nachrichten rechts, Team links) mit
/// Bild-/Sprachnachricht-Anhaengen und Messenger-Eingabezeile. Ziel des
/// Push-Deep-Links heimatplatz://feedback/{ticketId}; das Laden markiert
/// Team-Antworten als gelesen.
/// </summary>
[ShellMap<FeedbackThreadPage>("FeedbackThread")]
public partial class FeedbackThreadViewModel : ObservableObject, IPageLifecycleAware
{
    private const string RemoteAudioCacheSubfolder = "feedback-audio-remote";

    // Sprachnachrichten sind oeffentliche GUID-URLs - schlichter geteilter Client reicht
    private static readonly HttpClient AudioHttpClient = new();

    private readonly IFeedbackService _feedbackService;
    private readonly IAudioPlaybackService _playback;
    private readonly INavigator _navigator;
    private readonly IDialogs _dialogs;
    private readonly ILogger<FeedbackThreadViewModel> _logger;
    private readonly CommonStringsLocalized _commonLoc;

    private bool _loadedOnce;
    private bool _playbackHooked;

    public FeedbackThreadViewModel(
        IFeedbackService feedbackService,
        IAudioPlaybackService playback,
        FeedbackComposer composer,
        INavigator navigator,
        IDialogs dialogs,
        ILogger<FeedbackThreadViewModel> logger,
        FeedbackStringsLocalized loc,
        CommonStringsLocalized commonLoc)
    {
        _feedbackService = feedbackService;
        _playback = playback;
        _navigator = navigator;
        _dialogs = dialogs;
        _logger = logger;
        _commonLoc = commonLoc;
        Loc = loc;
        Composer = composer.Initialize();
    }

    public FeedbackStringsLocalized Loc { get; }

    public FeedbackComposer Composer { get; }

    /// <summary>Navigationsparameter: Id der zu ladenden Anfrage</summary>
    [ShellProperty]
    public string TicketId { get; set; } = string.Empty;

    public ObservableCollection<FeedbackMessageItem> Messages { get; } = [];

    [ObservableProperty]
    public partial string Subject { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StatusLabel { get; set; } = string.Empty;

    [ObservableProperty]
    public partial Color StatusColor { get; set; } = Colors.Transparent;

    [ObservableProperty]
    public partial string CategoryLabel { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CategoryIcon { get; set; } = string.Empty;

    [ObservableProperty]
    public partial Color CategoryColor { get; set; } = Colors.Transparent;

    [ObservableProperty]
    public partial string CreatedText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; set; }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    [ObservableProperty]
    public partial bool IsContentVisible { get; set; }

    public void OnAppearing()
    {
        if (!_playbackHooked)
        {
            _playbackHooked = true;
            _playback.PlaybackEnded += (_, _) =>
                MainThread.BeginInvokeOnMainThread(ResetPlayingStates);
        }

        // Bei Rueckkehr aus dem Hintergrund nicht neu laden - der Verlauf aendert
        // sich nur durch eigene Antworten (werden lokal angehaengt) oder Push (Reload via Tap)
        if (!_loadedOnce)
            _ = LoadAsync();
    }

    public void OnDisappearing()
    {
        Composer.Suspend();
        ResetPlayingStates();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (!Guid.TryParse(TicketId, out var ticketId))
        {
            ErrorMessage = Loc.NotFound;
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var ticket = await _feedbackService.GetTicketAsync(ticketId);
            if (ticket == null)
            {
                ErrorMessage = Loc.NotFound;
                IsContentVisible = false;
                return;
            }

            _loadedOnce = true;
            Subject = ticket.Subject;
            SetStatus(ticket.Status);
            CategoryLabel = FeedbackDisplay.CategoryLabel(ticket.Category, Loc);
            CategoryIcon = FeedbackDisplay.CategoryIcon(ticket.Category);
            CategoryColor = FeedbackDisplay.CategoryColor(ticket.Category);
            CreatedText = Loc.CreatedFormat(ticket.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy"));

            Messages.Clear();
            foreach (var message in ticket.Messages ?? [])
                Messages.Add(FeedbackDisplay.ToMessageItem(message, Loc));

            IsContentVisible = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Feedback] Laden des Verlaufs fehlgeschlagen: {TicketId}", TicketId);
            ErrorMessage = Loc.ThreadLoadFailed;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SendReplyAsync()
    {
        if (!Guid.TryParse(TicketId, out var ticketId))
            return;

        await Composer.FinishPendingRecordingAsync();

        if (!Composer.CanSend)
        {
            Composer.ErrorMessage = Loc.ErrorEmpty;
            return;
        }

        try
        {
            Composer.IsSending = true;
            Composer.ErrorMessage = null;

            var attachments = await Composer.UploadAttachmentsAsync();
            var message = await _feedbackService.AddMessageAsync(ticketId, Composer.Text.Trim(), attachments);
            if (message == null)
            {
                Composer.ErrorMessage = Loc.NotFound;
                return;
            }

            Messages.Add(FeedbackDisplay.ToMessageItem(message, Loc));
            Composer.Reset();

            // Nutzer-Antwort oeffnet die Anfrage serverseitig wieder
            SetStatus(ApiClient.Generated.FeedbackTicketStatus.Open);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Feedback] Antwort senden fehlgeschlagen: {TicketId}", TicketId);
            Composer.ErrorMessage = Loc.SendFailedFormat(ex.Message);
        }
        finally
        {
            Composer.IsSending = false;
        }
    }

    [RelayCommand]
    private async Task ToggleAudioAsync(FeedbackAudioItem item)
    {
        if (item.IsPlaying)
        {
            await _playback.StopAsync();
            ResetPlayingStates();
            return;
        }

        if (_playback.IsPlaying)
            await _playback.StopAsync();
        ResetPlayingStates();

        try
        {
            var localPath = await EnsureLocalAudioAsync(item.Url);
            item.IsPlaying = await _playback.PlayFileAsync(localPath);
            if (!item.IsPlaying)
                ErrorMessage = Loc.AudioPlayFailed;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Feedback] Sprachnachricht-Download fehlgeschlagen: {Url}", item.Url);
            ErrorMessage = Loc.AudioPlayFailed;
        }
    }

    [RelayCommand]
    private Task OpenImageAsync(FeedbackImageItem item)
        => Launcher.Default.OpenAsync(item.FullUrl);

    /// <summary>Auto-Titel ("Lob 1" etc.) der Anfrage umbenennen.</summary>
    [RelayCommand]
    private async Task RenameAsync()
    {
        if (!Guid.TryParse(TicketId, out var ticketId))
            return;

        var result = await _dialogs.Prompt(
            Loc.RenameTitle,
            Loc.RenameMessage,
            _commonLoc.Save,
            _commonLoc.Cancel,
            initialValue: Subject,
            maxLength: 200);

        var newSubject = result?.Trim();
        if (string.IsNullOrEmpty(newSubject) || newSubject == Subject)
            return;

        try
        {
            var success = await _feedbackService.RenameTicketAsync(ticketId, newSubject);
            if (success)
                Subject = newSubject;
            else
                await _dialogs.Alert(_commonLoc.Error, Loc.RenameFailed, _commonLoc.Ok);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Feedback] Umbenennen fehlgeschlagen: {TicketId}", TicketId);
            await _dialogs.Alert(_commonLoc.Error, Loc.RenameFailed, _commonLoc.Ok);
        }
    }

    [RelayCommand]
    private Task GoBackAsync() => _navigator.GoBack();

    private void SetStatus(ApiClient.Generated.FeedbackTicketStatus status)
    {
        StatusLabel = FeedbackDisplay.StatusLabel(status, Loc);
        StatusColor = FeedbackDisplay.StatusColor(status);
    }

    /// <summary>Laedt eine remote Sprachnachricht einmalig in den Cache (Dateiname = GUID aus der URL).</summary>
    private static async Task<string> EnsureLocalAudioAsync(string url)
    {
        var cacheDir = Path.Combine(FileSystem.CacheDirectory, RemoteAudioCacheSubfolder);
        Directory.CreateDirectory(cacheDir);

        var fileName = Path.GetFileName(new Uri(url).AbsolutePath);
        var localPath = Path.Combine(cacheDir, fileName);
        if (File.Exists(localPath))
            return localPath;

        var bytes = await AudioHttpClient.GetByteArrayAsync(url);
        await File.WriteAllBytesAsync(localPath, bytes);
        return localPath;
    }

    private void ResetPlayingStates()
    {
        foreach (var message in Messages)
        {
            foreach (var audio in message.Audios)
                audio.IsPlaying = false;
        }
    }
}
