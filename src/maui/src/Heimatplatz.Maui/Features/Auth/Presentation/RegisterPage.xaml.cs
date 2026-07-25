namespace Heimatplatz.Maui.Features.Auth.Presentation;

/// <summary>
/// Registrierungsseite
/// </summary>
public partial class RegisterPage : ContentPage
{
    private RegisterViewModel? _viewModel;

    public RegisterPage()
    {
        InitializeComponent();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (_viewModel != null)
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

        _viewModel = BindingContext as RegisterViewModel;
        if (_viewModel != null)
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    /// <summary>
    /// Der Fehlerbanner sitzt ueber dem Formular, der "Konto erstellen"-Button ganz
    /// unten - ohne Scroll wirkt ein Validierungsfehler wie "nichts passiert".
    /// </summary>
    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RegisterViewModel.ErrorMessage) &&
            !string.IsNullOrEmpty(_viewModel?.ErrorMessage))
            Dispatcher.Dispatch(async () => await RegisterScroll.ScrollToAsync(0, 0, animated: true));
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
