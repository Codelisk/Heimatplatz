using Microsoft.Maui.Controls.Shapes;

namespace Heimatplatz.Maui.Controls;

/// <summary>
/// Rot-weiss-rote Oesterreich-Flagge - Markenelement aus dem Heimatplatz-Signet.
/// Echte Flaggen-Proportion 2:3 (Hoehe:Breite) statt gestrecktem Band, damit sie
/// nicht gestaucht wirkt. Sparsam einsetzen: Flyout-Header, Sektions-Auftakt.
/// </summary>
public class FlagBand : Border
{
    public FlagBand()
    {
        StrokeShape = new RoundRectangle { CornerRadius = 3 };
        StrokeThickness = 1;
        Padding = 0;
        WidthRequest = 24;
        HeightRequest = 16;
        HorizontalOptions = LayoutOptions.Start;
        VerticalOptions = LayoutOptions.Center;

        // Haarlinie wie der Zettel-Fotorand (ZettelFrame/ZettelFrameDark): fasst den
        // weissen Mittelstreifen auf hellen Flaechen ein, im Dark Mode zarter Weisston
        this.SetAppTheme<Brush>(
            StrokeProperty,
            new SolidColorBrush(Color.FromArgb("#2E362B24")),
            new SolidColorBrush(Color.FromArgb("#29F3EDE7")));

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
