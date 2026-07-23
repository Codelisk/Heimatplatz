using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Maui.Features.Auth;
using Heimatplatz.Maui.Features.Feedback.Models;
using Heimatplatz.Maui.Features.Feedback.Services;
using Heimatplatz.Maui.Localization.Feedback;
using Microsoft.Extensions.Logging;
using Shiny;

namespace Heimatplatz.Maui.Features.Feedback.Presentation;

/// <summary>
/// "Anschlagtafel fuer Anliegen": Schnellstart-Kacheln (Kategorie antippen -> direkt
/// zur Nachrichten-Seite, kein Zwischenschritt) und darunter die eigenen Anfragen als
/// Zettel mit Kategorie-Icon und Status-Ampel. Shell-Root (Flyout-Eintrag "Feedback").
/// Umbenennen des Auto-Titels lebt im Verlauf (<see cref="FeedbackThreadViewModel"/>).
/// </summary>
[ShellMap<FeedbackListPage>("Feedback", registerRoute: false)]
public partial class FeedbackListViewModel(
    IFeedbackService feedbackService,
    IAuthService authService,
    INavigator navigator,
    ILogger<FeedbackListViewModel> logger,
    FeedbackStringsLocalized loc
) : ObservableObject, IPageLifecycleAware
{
    public FeedbackStringsLocalized Loc { get; } = loc;

    public ObservableCollection<FeedbackTicketListItem> Tickets { get; } = [];

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool IsRefreshing { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLoggedIn))]
    public partial bool IsLoggedOut { get; set; }

    public bool IsLoggedIn => !IsLoggedOut;

    [ObservableProperty]
    public partial bool HasLoadError { get; set; }

    [ObservableProperty]
    public partial bool ShowEmptyState { get; set; }

    [ObservableProperty]
    public partial bool HasTickets { get; set; }

    public void OnAppearing()
    {
        IsLoggedOut = !authService.IsAuthenticated;
        if (IsLoggedIn)
            _ = LoadAsync();
    }

    public void OnDisappearing()
    {
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsBusy)
        {
            IsRefreshing = false;
            return;
        }

        try
        {
            IsBusy = true;
            HasLoadError = false;
            ShowEmptyState = false;

            var response = await feedbackService.GetMyTicketsAsync();

            Tickets.Clear();
            foreach (var ticket in response.Tickets ?? [])
            {
                Tickets.Add(new FeedbackTicketListItem
                {
                    Id = ticket.Id,
                    Subject = ticket.Subject,
                    Preview = ticket.LastMessagePreview ?? string.Empty,
                    StatusLabel = FeedbackDisplay.StatusLabel(ticket.Status, Loc),
                    StatusColor = FeedbackDisplay.StatusColor(ticket.Status),
                    CategoryIcon = FeedbackDisplay.CategoryIcon(ticket.Category),
                    CategoryColor = FeedbackDisplay.CategoryColor(ticket.Category),
                    DateText = FeedbackDisplay.FormatRelativeDate(ticket.LastMessageAt, Loc),
                    MessageCountText = ticket.MessageCount == 1
                        ? Loc.MessageCountOne
                        : Loc.MessageCountFormat(ticket.MessageCount),
                    HasUnread = ticket.HasUnread
                });
            }

            HasTickets = Tickets.Count > 0;
            ShowEmptyState = Tickets.Count == 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Feedback] Laden der Anfragen fehlgeschlagen");
            HasLoadError = true;
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    /// <summary>Schnellstart-Kachel: Kategorie (Enum-Name) -> direkt zur Nachrichten-Seite.</summary>
    [RelayCommand]
    private Task StartFeedbackAsync(string category)
        => navigator.NavigateTo<FeedbackNewMessageViewModel>(vm => vm.Category = category);

    [RelayCommand]
    private Task OpenTicketAsync(FeedbackTicketListItem item)
        => navigator.NavigateTo<FeedbackThreadViewModel>(vm => vm.TicketId = item.Id.ToString("D"));

    [RelayCommand]
    private Task GoToLoginAsync()
        => navigator.NavigateTo("Login", relativeNavigation: false);
}
