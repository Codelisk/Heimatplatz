namespace Heimatplatz.App.Presentation;

/// <summary>
/// Datenschutzerklaerung Seite
/// </summary>
public sealed partial class PrivacyPolicyPage : Page
{
    public PrivacyPolicyViewModel ViewModel => (PrivacyPolicyViewModel)DataContext;

    public PrivacyPolicyPage()
    {
        this.InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Lade Datenschutzerklaerung beim ersten Laden
        if (ViewModel is { PrivacyPolicy: null })
        {
            await ViewModel.LoadAsync();
        }
    }

    private void OnBackClick(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
    }
}
