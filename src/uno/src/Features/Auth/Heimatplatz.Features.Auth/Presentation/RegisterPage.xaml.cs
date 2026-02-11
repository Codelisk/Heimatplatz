using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace Heimatplatz.Features.Auth.Presentation;

/// <summary>
/// Registrierungsseite
/// </summary>
public sealed partial class RegisterPage : Page
{
    public RegisterPage()
    {
        InitializeComponent();
    }

    private void PasswordBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter && DataContext is RegisterViewModel vm && vm.RegisterCommand.CanExecute(null))
        {
            vm.RegisterCommand.Execute(null);
        }
    }
}
