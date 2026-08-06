using Heimatplatz.Api.Features.Dashboards.Contracts.Models;

namespace Heimatplatz.Api.Features.Dashboards.Services.Widgets;

/// <summary>
/// Gemeinsame Bereinigungs-Bausteine der Widget-Resolver (fail-closed).
/// </summary>
internal static class WidgetSanitizeHelpers
{
    private static readonly string[] AllowedSizes =
        [DashboardWidgetSizes.S, DashboardWidgetSizes.M, DashboardWidgetSizes.L, DashboardWidgetSizes.Full];

    /// <summary>Normalisiert die semantische Groesse; Unbekanntes faellt auf den Widget-Default.</summary>
    public static string NormalizeSize(string? size, string fallback)
    {
        var normalized = size?.Trim().ToLowerInvariant();
        return normalized is not null && AllowedSizes.Contains(normalized) ? normalized : fallback;
    }

    /// <summary>Widget-Titel trimmen und kappen; leer wird null (Renderer laesst die Ueberschrift weg).</summary>
    public static string? NormalizeTitle(string? title)
    {
        var trimmed = title?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return null;
        return trimmed.Length > 80 ? trimmed[..80] : trimmed;
    }
}
