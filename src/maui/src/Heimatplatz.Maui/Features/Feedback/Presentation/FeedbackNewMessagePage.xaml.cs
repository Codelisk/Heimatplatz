using Heimatplatz.Maui.Features.Feedback.Controls;

namespace Heimatplatz.Maui.Features.Feedback.Presentation;

public partial class FeedbackNewMessagePage : ContentPage
{
    public FeedbackNewMessagePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ComposerSoftInput.Engage();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        ComposerSoftInput.Restore();
    }
}
