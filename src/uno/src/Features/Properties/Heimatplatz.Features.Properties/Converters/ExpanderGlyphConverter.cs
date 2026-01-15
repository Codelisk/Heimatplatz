using Microsoft.UI.Xaml.Data;

namespace Heimatplatz.Features.Properties.Converters;

/// <summary>
/// Konvertiert bool (IsExpanded) zu Glyph-String für Expander-Icon
/// </summary>
public class ExpanderGlyphConverter : IValueConverter
{
    // ▶ collapsed, ▼ expanded
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isExpanded)
        {
            return isExpanded ? "\uE70D" : "\uE76C"; // ChevronDown : ChevronRight
        }
        return "\uE76C";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
