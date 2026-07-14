using System.Collections.ObjectModel;
using Shiny.Maui.Controls;

namespace Heimatplatz.Maui.Features.Properties.Presentation;

public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();

        // Detents im Code ersetzen: XAML-Kollektions-Syntax ADDIERT zu den
        // Defaults (Quarter/Half/Full) - das Panel wuerde sonst am kleinsten
        // Detent (25%) oeffnen und die Browse-Liste kollabiert.
        OrtPanel.Detents = new ObservableCollection<DetentValue>
        {
            new(0.7),
            DetentValue.Full
        };
    }
}
