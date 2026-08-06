namespace Heimatplatz.Api.Features.Dashboards.Services;

/// <summary>
/// Fachlicher Validierungs-Fehlschlag einer KI-Definition. Die Message ist
/// bewusst nutzerfreundlich formuliert - sie landet (beim endgueltigen
/// Job-Fehlschlag) als GenerationError direkt beim Nutzer.
/// </summary>
public class DashboardValidationException(string message) : Exception(message);
