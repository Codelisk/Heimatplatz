using UnoFramework.Pages;

namespace Heimatplatz.Features.Auth.Presentation;

/// <summary>
/// UserProfilePage - Mein Profil Seite
/// </summary>
public sealed partial class UserProfilePage : BasePage
{
    public UserProfileViewModel? ViewModel => DataContext as UserProfileViewModel;

    public UserProfilePage()
    {
        this.InitializeComponent();
    }
}
