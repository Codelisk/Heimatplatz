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
/// Neues Feedback: die Kategorie kommt direkt von der Schnellstart-Kachel der Liste
/// (kein Zwischenschritt), oben zeigt ein Hero Icon/Titel/Leitfrage der Kategorie,
/// unten haengt die Messenger-Eingabezeile (<see cref="Composer"/>) als einziges Feld
/// der Seite am Rand. Nach dem Senden vergibt der Server einen Auto-Titel
/// (z.B. "Lob 1"), umbenennbar im Verlauf.
/// </summary>
[ShellMap<FeedbackNewMessagePage>("FeedbackNewMessage")]
public partial class FeedbackNewMessageViewModel : ObservableObject, IPageLifecycleAware
{
    private readonly IFeedbackService _feedbackService;
    private readonly INavigator _navigator;
    private readonly ILogger<FeedbackNewMessageViewModel> _logger;

    private FeedbackCategory _category = FeedbackCategory.Idea;

    public FeedbackNewMessageViewModel(
        IFeedbackService feedbackService,
        FeedbackComposer composer,
        INavigator navigator,
        ILogger<FeedbackNewMessageViewModel> logger,
        FeedbackStringsLocalized loc)
    {
        _feedbackService = feedbackService;
        _navigator = navigator;
        _logger = logger;
        Loc = loc;
        Composer = composer.Initialize();
        ApplyCategory(_category);
    }

    public FeedbackStringsLocalized Loc { get; }

    public FeedbackComposer Composer { get; }

    /// <summary>Navigationsparameter: auf der Liste angetippte Kategorie (Enum-Name).</summary>
    [ShellProperty]
    public string Category
    {
        get => _category.ToString();
        set
        {
            _category = Enum.TryParse<FeedbackCategory>(value, out var parsed) ? parsed : FeedbackCategory.Idea;
            ApplyCategory(_category);
        }
    }

    [ObservableProperty]
    public partial string CategoryTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CategoryHint { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CategoryIcon { get; set; } = string.Empty;

    [ObservableProperty]
    public partial Color CategoryColor { get; set; } = Colors.Transparent;

    public void OnAppearing()
    {
    }

    public void OnDisappearing() => Composer.Suspend();

    private void ApplyCategory(FeedbackCategory category)
    {
        // Volles Kategorie-Label als Hero-Titel (die Kacheln nutzen die Kurzform)
        CategoryTitle = FeedbackDisplay.CategoryLabel(category, Loc);
        CategoryHint = FeedbackDisplay.CategoryHint(category, Loc);
        CategoryIcon = FeedbackDisplay.CategoryIcon(category);
        CategoryColor = FeedbackDisplay.CategoryColor(category);
    }

    [RelayCommand]
    private async Task SendAsync()
    {
        // Eine noch laufende Aufnahme gehoert mit dazu
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
            await _feedbackService.CreateTicketAsync(_category, Composer.Text.Trim(), attachments);

            Composer.Reset();
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
