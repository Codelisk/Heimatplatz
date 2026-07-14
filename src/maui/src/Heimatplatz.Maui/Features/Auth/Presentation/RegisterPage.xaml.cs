namespace Heimatplatz.Maui.Features.Auth.Presentation;

/// <summary>
/// Registrierungsseite
/// </summary>
public partial class RegisterPage : ContentPage
{
    public RegisterPage()
    {
        InitializeComponent();
    }

    private void OnVornameCompleted(object? sender, EventArgs e) => NachnameEntry.Focus();

    private void OnNachnameCompleted(object? sender, EventArgs e) => EmailEntry.Focus();

    private void OnEmailCompleted(object? sender, EventArgs e) => PasswortEntry.Focus();

    private void OnPasswortCompleted(object? sender, EventArgs e) => PasswortBestaetigungEntry.Focus();
}
