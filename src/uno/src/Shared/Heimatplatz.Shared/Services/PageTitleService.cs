using CommunityToolkit.Mvvm.ComponentModel;

namespace Heimatplatz.Services;

/// <summary>
/// Service zur Verwaltung des aktuellen Page-Titels und optionaler Header-Inhalte
/// </summary>
[Service(UnoService.Lifetime, TryAdd = UnoService.TryAdd)]
public partial class PageTitleService : ObservableObject
{
    [ObservableProperty]
    private string _currentTitle = "HEIMATPLATZ";

    [ObservableProperty]
    private object? _headerContent;

    /// <summary>
    /// Setzt den aktuellen Page-Titel
    /// </summary>
    /// <param name="title">Der neue Titel (null oder leer setzt "HEIMATPLATZ" als Fallback)</param>
    public void SetTitle(string? title)
    {
        CurrentTitle = string.IsNullOrWhiteSpace(title) ? "HEIMATPLATZ" : title;
    }

    /// <summary>
    /// Setzt den optionalen Header-Content (z.B. Action Buttons)
    /// </summary>
    /// <param name="content">Der Header-Content (null entfernt den Content)</param>
    public void SetHeaderContent(object? content)
    {
        HeaderContent = content;
    }

    /// <summary>
    /// Setzt Titel und Header-Content gleichzeitig
    /// </summary>
    public void SetPage(string? title, object? headerContent = null)
    {
        SetTitle(title);
        SetHeaderContent(headerContent);
    }

    /// <summary>
    /// Resettet auf Standard-Werte
    /// </summary>
    public void Reset()
    {
        CurrentTitle = "HEIMATPLATZ";
        HeaderContent = null;
    }
}
