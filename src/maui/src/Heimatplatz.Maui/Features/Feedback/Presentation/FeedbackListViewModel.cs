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
/// "Meine Anfragen": Liste der eigenen Feedback-Tickets mit Status/Unread-Badges,
/// Einstieg in Compose und Verlauf. Shell-Root (Flyout-Eintrag "Feedback").
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
    [NotifyPropertyChangedFor(nameof(IsLoggedIn))]
    public partial bool IsLoggedOut { get; set; }

    public bool IsLoggedIn => !IsLoggedOut;

    [ObservableProperty]
    public partial bool HasLoadError { get; set; }

    [ObservableProperty]
    public partial bool ShowEmptyState { get; set; }

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
            return;

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
                    CategoryLabel = FeedbackDisplay.CategoryLabel(ticket.Category, Loc),
                    DateText = FeedbackDisplay.FormatDate(ticket.LastMessageAt),
                    MessageCountText = Loc.MessageCountFormat(ticket.MessageCount),
                    HasUnread = ticket.HasUnread
                });
            }

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
        }
    }

    [RelayCommand]
    private Task NewFeedbackAsync()
        => navigator.NavigateTo<FeedbackComposeViewModel>();

    [RelayCommand]
    private Task OpenTicketAsync(FeedbackTicketListItem item)
        => navigator.NavigateTo<FeedbackThreadViewModel>(vm => vm.TicketId = item.Id.ToString("D"));

    [RelayCommand]
    private Task GoToLoginAsync()
        => navigator.NavigateTo("Login", relativeNavigation: false);
}
