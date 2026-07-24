using System.Text.RegularExpressions;

namespace Heimatplatz.Maui.Controls;

/// <summary>
/// Attached Property fuer CMS-Fliesstext (Impressum/Datenschutz-Abschnitte): erkennt
/// E-Mail-Adressen und http(s)-URLs im gebundenen Text und macht sie als Spans antippbar
/// (mailto:/Launcher). Attached statt Label-Subklasse, damit der implizite Label-Style
/// (Theme-Textfarbe) weiter greift. Telefonnummern werden bewusst NICHT erkannt: im
/// Fliesstext von Rechtstexten (Paragraphen, Datumsangaben) sind Fehlalarme zu haeufig -
/// strukturierte Kontaktdaten verlinkt die jeweilige Seite explizit.
/// </summary>
public static class AutoLink
{
    public static readonly BindableProperty TextProperty = BindableProperty.CreateAttached(
        "Text",
        typeof(string),
        typeof(AutoLink),
        default(string),
        propertyChanged: OnTextChanged);

    private static readonly Regex TokenRegex = new(
        @"(?<email>[\w.+-]+@[\w-]+(\.[\w-]+)+)|(?<url>https?://[^\s]+)",
        RegexOptions.Compiled);

    public static string? GetText(BindableObject view) => (string?)view.GetValue(TextProperty);

    public static void SetText(BindableObject view, string? value) => view.SetValue(TextProperty, value);

    private static void OnTextChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not Label label)
            return;

        var text = newValue as string;
        if (string.IsNullOrEmpty(text))
        {
            label.FormattedText = null;
            label.Text = null;
            return;
        }

        var matches = TokenRegex.Matches(text);
        if (matches.Count == 0)
        {
            label.FormattedText = null;
            label.Text = text;
            return;
        }

        var formatted = new FormattedString();
        var lastIndex = 0;

        foreach (Match match in matches)
        {
            if (match.Index > lastIndex)
                formatted.Spans.Add(new Span { Text = text[lastIndex..match.Index] });

            var isEmail = match.Groups["email"].Success;

            // URLs am Satzende: schliessende Satzzeichen gehoeren nicht zum Link,
            // bleiben aber als normaler Text erhalten.
            var value = isEmail ? match.Value : match.Value.TrimEnd('.', ',', ';', ':', '!', '?', ')', '"', '\'');

            formatted.Spans.Add(BuildLinkSpan(value, isEmail));

            if (value.Length < match.Length)
                formatted.Spans.Add(new Span { Text = match.Value[value.Length..] });

            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < text.Length)
            formatted.Spans.Add(new Span { Text = text[lastIndex..] });

        label.Text = null;
        label.FormattedText = formatted;
    }

    private static Span BuildLinkSpan(string value, bool isEmail)
    {
        var span = new Span
        {
            Text = value,
            TextDecorations = TextDecorations.Underline
        };

        if (Application.Current is { } app &&
            app.Resources.TryGetValue("Primary", out var light) && light is Color lightColor &&
            app.Resources.TryGetValue("PrimaryDark", out var dark) && dark is Color darkColor)
        {
            span.SetAppThemeColor(Span.TextColorProperty, lightColor, darkColor);
        }

        var uri = isEmail ? $"mailto:{value}" : value;

        span.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () =>
            {
                try
                {
                    await Launcher.Default.OpenAsync(uri);
                }
                catch
                {
                    // Keine passende App registriert - der Tap bleibt folgenlos.
                }
            })
        });

        return span;
    }
}
