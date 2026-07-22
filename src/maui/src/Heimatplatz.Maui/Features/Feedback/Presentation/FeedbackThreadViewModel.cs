using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Maui.Features.Feedback.Models;
using Heimatplatz.Maui.Features.Feedback.Services;
using Heimatplatz.Maui.Localization.Feedback;
using Microsoft.Extensions.Logging;
using Shiny;

namespace Heimatplatz.Maui.Features.Feedback.Presentation;

/// <summary>
/// Verlauf einer Anfrage als Chat (eigene Nachrichten rechts, Team links) mit
/// Bild-/Sprachnachricht-Anhaengen und Antwort-Zeile. Ziel des Push-Deep-Links
/// heimatplatz://feedback/{ticketId}; das Laden markiert Team-Antworten als gelesen.
/// </summary>
[ShellMap<FeedbackThreadPage>("FeedbackThread")]
public partial class FeedbackThreadViewModel(
    IFeedbackService feedbackService,
    IAudioPlaybackService playback,
    INavigator navigator,
    ILogger<FeedbackThreadViewModel> logger,
    FeedbackStringsLocalized loc
) : ObservableObject, IPageLifecycleAware
{
    private const string RemoteAudioCacheSubfolder = "feedback-audio-remote";

    // Sprachnachrichten sind oeffentliche GUID-URLs - schlichter geteilter Client reicht
    private static readonly HttpClient AudioHttpClient = new();

    private bool _loadedOnce;
    private bool _playbackHooked;

    public FeedbackStringsLocalized Loc { get; } = loc;

    /// <summary>Navigationsparameter: Id der zu ladenden Anfrage</summary>
    [ShellProperty]
    public string TicketId { get; set; } = string.Empty;

    public ObservableCollection<FeedbackMessageItem> Messages { get; } = [];

    [ObservableProperty]
    public partial string Subject { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StatusLabel { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CategoryLabel { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; set; }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    [ObservableProperty]
    public partial bool IsContentVisible { get; set; }

    [ObservableProperty]
    public partial string ReplyText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotSending))]
    public partial bool IsSending { get; set; }

    public bool IsNotSending => !IsSending;

    public void OnAppearing()
    {
        if (!_playbackHooked)
        {
            _playbackHooked = true;
            playback.PlaybackEnded += (_, _) =>
                MainThread.BeginInvokeOnMainThread(ResetPlayingStates);
        }

        // Bei Rueckkehr aus dem Hintergrund nicht neu laden - der Verlauf aendert
        // sich nur durch eigene Antworten (werden lokal angehaengt) oder Push (Reload via Tap)
        if (!_loadedOnce)
            _ = LoadAsync();
    }

    public void OnDisappearing()
    {
        if (playback.IsPlaying)
            _ = playback.StopAsync();
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

            var ticket = await feedbackService.GetTicketAsync(ticketId);
            if (ticket == null)
            {
                ErrorMessage = Loc.NotFound;
                IsContentVisible = false;
                return;
            }

            _loadedOnce = true;
            Subject = ticket.Subject;
            StatusLabel = FeedbackDisplay.StatusLabel(ticket.Status, Loc);
            CategoryLabel = FeedbackDisplay.CategoryLabel(ticket.Category, Loc);

            Messages.Clear();
            foreach (var message in ticket.Messages ?? [])
                Messages.Add(FeedbackDisplay.ToMessageItem(message, Loc));

            IsContentVisible = true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Feedback] Laden des Verlaufs fehlgeschlagen: {TicketId}", TicketId);
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
        var body = ReplyText.Trim();
        if (body.Length == 0 || !Guid.TryParse(TicketId, out var ticketId))
            return;

        try
        {
            IsSending = true;
            var message = await feedbackService.AddMessageAsync(ticketId, body, []);
            if (message == null)
            {
                ErrorMessage = Loc.NotFound;
                return;
            }

            Messages.Add(FeedbackDisplay.ToMessageItem(message, Loc));
            ReplyText = string.Empty;

            // Nutzer-Antwort oeffnet die Anfrage serverseitig wieder
            StatusLabel = FeedbackDisplay.StatusLabel(ApiClient.Generated.FeedbackTicketStatus.Open, Loc);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Feedback] Antwort senden fehlgeschlagen: {TicketId}", TicketId);
            ErrorMessage = Loc.SendFailedFormat(ex.Message);
        }
        finally
        {
            IsSending = false;
        }
    }

    [RelayCommand]
    private async Task ToggleAudioAsync(FeedbackAudioItem item)
    {
        if (item.IsPlaying)
        {
            await playback.StopAsync();
            ResetPlayingStates();
            return;
        }

        if (playback.IsPlaying)
            await playback.StopAsync();
        ResetPlayingStates();

        try
        {
            var localPath = await EnsureLocalAudioAsync(item.Url);
            item.IsPlaying = await playback.PlayFileAsync(localPath);
            if (!item.IsPlaying)
                ErrorMessage = Loc.AudioPlayFailed;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Feedback] Sprachnachricht-Download fehlgeschlagen: {Url}", item.Url);
            ErrorMessage = Loc.AudioPlayFailed;
        }
    }

    [RelayCommand]
    private Task OpenImageAsync(FeedbackImageItem item)
        => Launcher.Default.OpenAsync(item.FullUrl);

    [RelayCommand]
    private Task GoBackAsync() => navigator.GoBack();

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
