using Heimatplatz.Api.Features.Dashboards.Contracts.Models;

namespace Heimatplatz.Api.Features.Dashboards.Services.Widgets;

/// <summary>
/// Statischer KI-Text (text-note): Einordnung oder Hinweis der KI. Der Text steht
/// in der Definition (keine Live-Daten, kein KI-Aufruf beim Rendern) und wird von
/// den Renderern immer escaped ausgegeben.
/// </summary>
public class TextNoteWidgetResolver : IDashboardWidgetResolver
{
    public const int MaxTextLength = 500;

    public string Kind => DashboardWidgetKinds.TextNote;

    public WidgetDescriptor Descriptor => new(
        Kind,
        "Kurzer statischer Hinweis-/Einordnungstext (kein Datenbezug).",
        $"options.text (Pflicht, max. {MaxTextLength} Zeichen, deutsche Sie-Form). query wird ignoriert.");

    public DashboardWidget? Sanitize(DashboardWidget widget, List<string> warnings)
    {
        var text = widget.Options?.Text?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            warnings.Add("text-note ohne Text verworfen.");
            return null;
        }

        widget.Query = null;
        widget.Size = WidgetSanitizeHelpers.NormalizeSize(widget.Size, DashboardWidgetSizes.Full);
        widget.Title = WidgetSanitizeHelpers.NormalizeTitle(widget.Title);
        widget.Options = new DashboardWidgetOptions
        {
            Text = text.Length > MaxTextLength ? text[..MaxTextLength] : text
        };

        return widget;
    }

    public Task<WidgetDataDto> ResolveAsync(DashboardWidget widget, CancellationToken cancellationToken) =>
        Task.FromResult(new WidgetDataDto(
            widget.Id, Kind, Success: true, Error: null,
            TextNote: new TextNoteWidgetData(widget.Options?.Text ?? "")));
}
