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

    private void OnSellerTypeTapped(object? sender, TappedEventArgs e)
    {
        if (BindingContext is not RegisterViewModel viewModel || e.Parameter is not string sellerType)
            return;

        switch (sellerType)
        {
            case "Private":
                viewModel.IsPrivateSeller = true;
                break;
            case "Broker":
                viewModel.IsBrokerSeller = true;
                break;
            case "PropertyManager":
                viewModel.IsPropertyManagerSeller = true;
                break;
        }
    }
}
