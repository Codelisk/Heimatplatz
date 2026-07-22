using Heimatplatz.Maui.Features.Feedback.Controls;

namespace Heimatplatz.Maui.Features.Feedback.Presentation;

public partial class FeedbackThreadPage : ContentPage
{
    public FeedbackThreadPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ComposerSoftInput.UseResize();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        ComposerSoftInput.RestorePan();
    }
}
