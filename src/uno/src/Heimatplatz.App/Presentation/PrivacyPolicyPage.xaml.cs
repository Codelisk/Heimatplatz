using UnoFramework.Pages;

namespace Heimatplatz.App.Presentation;

/// <summary>
/// Datenschutzerklaerung Seite
/// </summary>
public sealed partial class PrivacyPolicyPage : BasePage
{
    public PrivacyPolicyViewModel ViewModel => (PrivacyPolicyViewModel)DataContext;

    public PrivacyPolicyPage()
    {
        this.InitializeComponent();
    }
}
