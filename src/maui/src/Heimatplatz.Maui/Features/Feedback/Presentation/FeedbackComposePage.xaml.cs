using Heimatplatz.Maui.Features.Feedback.Controls;

namespace Heimatplatz.Maui.Features.Feedback.Presentation;

public partial class FeedbackComposePage : ContentPage
{
    public FeedbackComposePage()
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
