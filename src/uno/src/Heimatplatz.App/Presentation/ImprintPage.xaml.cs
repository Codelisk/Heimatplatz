using UnoFramework.Pages;

namespace Heimatplatz.App.Presentation;

/// <summary>
/// Impressum Seite
/// </summary>
public sealed partial class ImprintPage : BasePage
{
    public ImprintViewModel ViewModel => (ImprintViewModel)DataContext;

    public ImprintPage()
    {
        this.InitializeComponent();
    }
}
