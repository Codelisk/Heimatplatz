using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace Heimatplatz.Features.Auth.Presentation;

/// <summary>
/// Anmeldeseite
/// </summary>
public sealed partial class LoginPage : Page
{
    public LoginPage()
    {
        InitializeComponent();
    }

    private void PasswordBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter && DataContext is LoginViewModel vm && vm.LoginCommand.CanExecute(null))
        {
            vm.LoginCommand.Execute(null);
        }
    }
}
