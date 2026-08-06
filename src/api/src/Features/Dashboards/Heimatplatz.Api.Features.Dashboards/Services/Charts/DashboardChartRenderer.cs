using System.Globalization;
using System.Reflection;
using Heimatplatz.Api;
using Shiny;
using SkiaSharp;

namespace Heimatplatz.Api.Features.Dashboards.Services.Charts;

/// <summary>
/// Malt die price-chart-Diagramme serverseitig per SkiaSharp (gleiche Library wie
/// die Foto-Pipeline) und liefert sie als data:-URIs - die Frontends zeigen nur
/// ein Bild, null Client-Bundle, in Web und MAUI identisch. Gerendert wird in
/// 2x-Aufloesung (Retina) auf transparentem Grund; die Karte drumherum liefert
/// den Hintergrund, Farben kommen je Theme-Variante aus einer festen Palette
/// (Markenrot #de2a2f wie BRAND_RED der Karten-Styles).
/// Schrift: eingebettetes Roboto-Regular (Apache 2.0) - die Linux-Container
/// haben keine Systemschriften, SkiaSharp braucht eine mitgelieferte.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class DashboardChartRenderer
{
    // 2x von ~620x280 CSS-Pixeln (Widget-Breite m/l im 12er-Grid)
    private const int Width = 1240;
    private const int Height = 560;
    private const float PaddingX = 24;
    private const float PaddingTop = 44;
    private const float PaddingBottom = 64;
    private const float AxisFontSize = 26;
    private const float CountFontSize = 24;

    private const int HistogramBuckets = 7;
    public const int WeeksWindow = 8;

    private static readonly Lazy<SKTypeface> Typeface = new(LoadEmbeddedTypeface);

    private sealed record Palette(SKColor Text, SKColor Grid, SKColor Bar);

    // Angenaehert an die starwind-Tokens: Light = Manila-Papier, Dark = warmes Fast-Schwarz
    private static readonly Palette Light = new(
        Text: new SKColor(0x57, 0x53, 0x4E),
        Grid: new SKColor(0xD8, 0xD2, 0xC7),
        Bar: new SKColor(0xDE, 0x2A, 0x2F));

    private static readonly Palette Dark = new(
        Text: new SKColor(0xB8, 0xB2, 0xA7),
        Grid: new SKColor(0x45, 0x41, 0x3B),
        Bar: new SKColor(0xDE, 0x2A, 0x2F));

    /// <summary>Preisverteilung der Treffermenge als Balken-Histogramm.</summary>
    public string RenderPriceHistogramDataUri(IReadOnlyList<decimal> prices, bool dark)
    {
        if (prices.Count == 0)
            throw new ArgumentException("Histogramm braucht mindestens einen Preis.", nameof(prices));

        var min = prices.Min();
        var max = prices.Max();
        var bucketWidth = NiceBucketWidth(min, max, HistogramBuckets);
        var start = Math.Floor(min / bucketWidth) * bucketWidth;

        var bucketCount = Math.Max(1, (int)Math.Ceiling(((double)max - (double)start + 1) / (double)bucketWidth));
        var counts = new int[bucketCount];
        foreach (var price in prices)
        {
            var index = Math.Clamp((int)(((double)price - (double)start) / (double)bucketWidth), 0, bucketCount - 1);
            counts[index]++;
        }

        var labels = new (int Index, string Text)[]
        {
            (0, FormatEuroCompact(start)),
            (bucketCount / 2, FormatEuroCompact(start + (decimal)(bucketCount / 2) * bucketWidth)),
            (bucketCount, FormatEuroCompact(start + bucketCount * bucketWidth))
        };

        return RenderBars(counts, dark, boundaryLabels: labels, barLabels: null);
    }

    /// <summary>Neuzugaenge pro Kalenderwoche (letzte <see cref="WeeksWindow"/> Wochen, aelteste links).</summary>
    public string RenderNewPerWeekDataUri(IReadOnlyList<DateTime> createdAtUtc, bool dark, DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        var thisMonday = StartOfIsoWeek(now);

        var counts = new int[WeeksWindow];
        var barLabels = new string[WeeksWindow];
        for (var i = 0; i < WeeksWindow; i++)
        {
            var weekStart = thisMonday.AddDays(-7 * (WeeksWindow - 1 - i));
            barLabels[i] = $"KW {ISOWeek.GetWeekOfYear(weekStart)}";
            var weekEnd = weekStart.AddDays(7);
            counts[i] = createdAtUtc.Count(d => d >= weekStart && d < weekEnd);
        }

        return RenderBars(counts, dark, boundaryLabels: null, barLabels: barLabels);
    }

    /// <summary>
    /// Gemeinsamer Balken-Renderer. boundaryLabels beschriften Bucket-GRENZEN
    /// (Histogramm), barLabels die Balken selbst (Wochen).
    /// </summary>
    private static string RenderBars(
        int[] counts,
        bool dark,
        (int Index, string Text)[]? boundaryLabels,
        string[]? barLabels)
    {
        var palette = dark ? Dark : Light;
        using var surface = SKSurface.Create(new SKImageInfo(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var axisFont = new SKFont(Typeface.Value, AxisFontSize);
        var countFont = new SKFont(Typeface.Value, CountFontSize);
        using var textPaint = new SKPaint { Color = palette.Text, IsAntialias = true };
        using var gridPaint = new SKPaint { Color = palette.Grid, IsAntialias = true, StrokeWidth = 2 };
        using var barPaint = new SKPaint { Color = palette.Bar, IsAntialias = true };

        var chartLeft = PaddingX;
        var chartRight = Width - PaddingX;
        var chartTop = PaddingTop;
        var chartBottom = Height - PaddingBottom;
        var chartWidth = chartRight - chartLeft;
        var chartHeight = chartBottom - chartTop;

        // Grundlinie + zwei dezente Hilfslinien
        canvas.DrawLine(chartLeft, chartBottom, chartRight, chartBottom, gridPaint);
        var maxCount = Math.Max(counts.Max(), 1);
        foreach (var fraction in new[] { 0.5f, 1f })
        {
            var y = chartBottom - chartHeight * fraction;
            using var faint = new SKPaint { Color = palette.Grid.WithAlpha(110), IsAntialias = true, StrokeWidth = 1.5f };
            canvas.DrawLine(chartLeft, y, chartRight, y, faint);
        }

        var slot = chartWidth / counts.Length;
        var gap = Math.Min(18f, slot * 0.18f);

        for (var i = 0; i < counts.Length; i++)
        {
            var x = chartLeft + i * slot;
            var barHeight = counts[i] == 0 ? 0f : Math.Max(chartHeight * counts[i] / maxCount, 6f);
            var rect = new SKRect(x + gap / 2, chartBottom - barHeight, x + slot - gap / 2, chartBottom);
            if (barHeight > 0)
                canvas.DrawRoundRect(rect, 8, 8, barPaint);

            // Anzahl ueber dem Balken (0 bleibt unbeschriftet, sonst wird es unruhig)
            if (counts[i] > 0)
            {
                canvas.DrawText(
                    counts[i].ToString(CultureInfo.InvariantCulture),
                    x + slot / 2, rect.Top - 10, SKTextAlign.Center, countFont, textPaint);
            }

            if (barLabels is not null)
            {
                canvas.DrawText(barLabels[i], x + slot / 2, chartBottom + AxisFontSize + 12, SKTextAlign.Center, axisFont, textPaint);
            }
        }

        if (boundaryLabels is not null)
        {
            foreach (var (index, text) in boundaryLabels)
            {
                var x = chartLeft + index * slot;
                var align = index == 0 ? SKTextAlign.Left : index >= counts.Length ? SKTextAlign.Right : SKTextAlign.Center;
                canvas.DrawText(text, x, chartBottom + AxisFontSize + 12, align, axisFont, textPaint);
            }
        }

        using var image = surface.Snapshot();
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        return $"data:image/png;base64,{Convert.ToBase64String(encoded.ToArray())}";
    }

    /// <summary>"Schoene" Bucket-Breite (1/2/2,5/5 x 10^n), damit die Grenzen rund sind.</summary>
    public static decimal NiceBucketWidth(decimal min, decimal max, int targetBuckets)
    {
        var range = Math.Max(max - min, 1);
        var raw = range / targetBuckets;
        var magnitude = (decimal)Math.Pow(10, Math.Floor(Math.Log10((double)raw)));
        foreach (var step in new[] { 1m, 2m, 2.5m, 5m, 10m })
        {
            if (magnitude * step >= raw)
                return magnitude * step;
        }
        return magnitude * 10m;
    }

    /// <summary>Kompaktes Euro-Format fuer Achsen: "450.000" bzw. "1,25 Mio."</summary>
    public static string FormatEuroCompact(decimal value)
    {
        var format = new NumberFormatInfo { NumberGroupSeparator = ".", NumberDecimalSeparator = ",", NumberDecimalDigits = 0 };
        if (Math.Abs(value) >= 1_000_000m)
        {
            var millions = value / 1_000_000m;
            var text = millions == Math.Truncate(millions)
                ? millions.ToString("0", format)
                : millions.ToString("0.##", format);
            return $"{text} Mio.";
        }
        return value.ToString("N0", format);
    }

    public static DateTime StartOfIsoWeek(DateTime utc)
    {
        var date = utc.Date;
        var diff = ((int)date.DayOfWeek + 6) % 7; // Montag = 0
        return date.AddDays(-diff);
    }

    private static SKTypeface LoadEmbeddedTypeface()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(
            "Heimatplatz.Api.Features.Dashboards.Resources.Roboto-Regular.ttf")
            ?? throw new InvalidOperationException("Eingebettete Diagramm-Schrift Roboto-Regular.ttf nicht gefunden.");
        return SKTypeface.FromStream(stream)
            ?? throw new InvalidOperationException("Diagramm-Schrift konnte nicht geladen werden.");
    }
}
