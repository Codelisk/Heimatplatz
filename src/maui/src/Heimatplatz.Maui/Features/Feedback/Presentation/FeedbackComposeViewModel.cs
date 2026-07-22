using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Features.Feedback.Models;
using Heimatplatz.Maui.Features.Feedback.Services;
using Heimatplatz.Maui.Localization.Feedback;
using Microsoft.Extensions.Logging;
using Shiny;

namespace Heimatplatz.Maui.Features.Feedback.Presentation;

/// <summary>
/// Neues Feedback: Kategorie-Chips und optionaler Betreff oben, darunter die
/// Messenger-Eingabezeile (<see cref="Composer"/>), an der Text, Bilder und
/// Sprachnachricht gemeinsam haengen.
/// </summary>
[ShellMap<FeedbackComposePage>("FeedbackCompose")]
public partial class FeedbackComposeViewModel : ObservableObject, IPageLifecycleAware
{
    private readonly IFeedbackService _feedbackService;
    private readonly INavigator _navigator;
    private readonly ILogger<FeedbackComposeViewModel> _logger;

    public FeedbackComposeViewModel(
        IFeedbackService feedbackService,
        FeedbackComposer composer,
        INavigator navigator,
        ILogger<FeedbackComposeViewModel> logger,
        FeedbackStringsLocalized loc)
    {
        _feedbackService = feedbackService;
        _navigator = navigator;
        _logger = logger;
        Loc = loc;
        Composer = composer.Initialize();

        Categories =
        [
            new FeedbackCategoryOption { Value = FeedbackCategory.Idea, Label = loc.CategoryIdea, IsSelected = true },
            new FeedbackCategoryOption { Value = FeedbackCategory.Problem, Label = loc.CategoryProblem },
            new FeedbackCategoryOption { Value = FeedbackCategory.Question, Label = loc.CategoryQuestion },
            new FeedbackCategoryOption { Value = FeedbackCategory.Praise, Label = loc.CategoryPraise },
            new FeedbackCategoryOption { Value = FeedbackCategory.Other, Label = loc.CategoryOther }
        ];
    }

    public FeedbackStringsLocalized Loc { get; }

    public FeedbackComposer Composer { get; }

    public ObservableCollection<FeedbackCategoryOption> Categories { get; }

    [ObservableProperty]
    public partial string Subject { get; set; } = string.Empty;

    public void OnAppearing()
    {
    }

    public void OnDisappearing() => Composer.Suspend();

    [RelayCommand]
    private void SelectCategory(FeedbackCategoryOption option)
    {
        foreach (var category in Categories)
            category.IsSelected = category == option;
    }

    [RelayCommand]
    private async Task SendAsync()
    {
        // Eine noch laufende Aufnahme gehoert mit dazu
        await Composer.FinishPendingRecordingAsync();

        var body = Composer.Text.Trim();
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
            var category = Categories.First(c => c.IsSelected).Value;
            var subject = string.IsNullOrWhiteSpace(Subject) ? null : Subject.Trim();

            await _feedbackService.CreateTicketAsync(category, subject, body, attachments);

            Composer.Reset();
            Subject = string.Empty;
            await _navigator.GoBack();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Feedback] Senden fehlgeschlagen");
            Composer.ErrorMessage = Loc.SendFailedFormat(ex.Message);
        }
        finally
        {
            Composer.IsSending = false;
        }
    }
}
