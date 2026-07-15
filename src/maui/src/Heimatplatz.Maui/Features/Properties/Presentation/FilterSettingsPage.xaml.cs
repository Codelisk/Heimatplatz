using System.Collections.ObjectModel;
using Shiny.Maui.Controls;

namespace Heimatplatz.Maui.Features.Properties.Presentation;

public partial class FilterSettingsPage : ContentPage
{
    public FilterSettingsPage()
    {
        InitializeComponent();

        OrtPanel.Detents = new ObservableCollection<DetentValue>
        {
            new(0.7),
            DetentValue.Full
        };
    }
}
