using Microsoft.Maui.Controls.Shapes;

namespace Heimatplatz.Maui.Controls;

/// <summary>
/// Rot-weiss-rotes Fahnenband - Markenelement aus dem Heimatplatz-Signet
/// (drei horizontale Streifen wie die Flagge im Logo). Sparsam einsetzen:
/// Flyout-Header, Sektions-Auftakt.
/// </summary>
public class FlagBand : Border
{
    public FlagBand()
    {
        StrokeShape = new RoundRectangle { CornerRadius = 3 };
        StrokeThickness = 0;
        Stroke = Colors.Transparent;
        Padding = 0;
        WidthRequest = 26;
        HeightRequest = 12;
        HorizontalOptions = LayoutOptions.Start;
        VerticalOptions = LayoutOptions.Center;

        var signalRed = Color.FromArgb("#DE2A2F");

        var grid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Star)
            }
        };

        grid.Add(new BoxView { Color = signalRed }, 0, 0);
        grid.Add(new BoxView { Color = Colors.White }, 0, 1);
        grid.Add(new BoxView { Color = signalRed }, 0, 2);

        Content = grid;
    }
}
