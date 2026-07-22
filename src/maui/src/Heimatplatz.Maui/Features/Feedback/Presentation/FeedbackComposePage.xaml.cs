using Heimatplatz.Maui.Features.Feedback.Controls;

namespace Heimatplatz.Maui.Features.Feedback.Presentation;

public partial class FeedbackComposePage : ContentPage
{
    public FeedbackComposePage()
    {
        InitializeComponent();

        // Beim Fokus aufs Nachrichtenfeld das Formular ans Ende scrollen: ueber der
        // gehobenen Eingabezeile steht dann der Hinweistext statt einer mittig
        // abgeschnittenen Betreff-Karte ("wo schreibe ich gerade?")
        ComposerView.EditorFocused += async (_, _) =>
        {
            await Task.Delay(250); // Tastatur-Anhebung abwarten, sonst stimmt die Zielposition nicht
            await FormScroll.ScrollToAsync(0, FormScroll.ContentSize.Height, animated: true);
        };
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
