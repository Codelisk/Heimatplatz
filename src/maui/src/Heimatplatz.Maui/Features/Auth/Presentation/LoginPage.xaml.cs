namespace Heimatplatz.Maui.Features.Auth.Presentation;

/// <summary>
/// Anmeldeseite
/// </summary>
public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
    }

    private void OnEmailCompleted(object? sender, EventArgs e) => PasswortEntry.Focus();
}
