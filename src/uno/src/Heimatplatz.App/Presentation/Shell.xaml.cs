using Microsoft.UI.Xaml.Controls;

namespace Heimatplatz.App.Presentation;

public sealed partial class Shell : UserControl, IContentControlProvider
{
    public ContentControl ContentControl => NavigationContent;

    public Shell()
    {
        this.InitializeComponent();
    }
}
