using Heimatplatz.Api.Cleanup;
using Heimatplatz.Api.Core.Data.Seeding.Configuration;
using Heimatplatz.Api.Features.Feedback.Data.Seeding;
using Heimatplatz.Api.Features.Feedback.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.Api.Features.Feedback.Configuration;

/// <summary>
/// DI-Registrierung fuer das Feedback Feature
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registriert die Services des Feedback Features: Nutzer-Anfragen (Wunsch/Problem/
    /// Frage/Lob) mit Bild-/Audio-Anhaengen, Team-Antworten im Intern-Bereich und
    /// Push-Benachrichtigung bei Antwort (via FeedbackTeamRepliedEvent im Notifications-Feature).
    /// </summary>
    public static IServiceCollection AddFeedbackFeature(this IServiceCollection services)
    {
        services.AddGeneratedServices();
        services.AddSeeder<FeedbackSeeder>();

        // Account-Loeschung: entfernt Anfragen, Verlauf und Anhang-Dateien des Benutzers.
        // Explizit (nicht via [Service]/TryAdd), damit IEnumerable<IUserDataEraser> alle Beitraege erhaelt.
        services.AddScoped<IUserDataEraser, FeedbackUserDataEraser>();

        return services;
    }
}
