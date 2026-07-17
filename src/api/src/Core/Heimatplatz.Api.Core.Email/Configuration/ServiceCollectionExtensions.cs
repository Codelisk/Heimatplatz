using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.Api.Core.Email.Configuration;

/// <summary>
/// DI-Registrierung fuer den E-Mail-Versand
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registriert IEmailSender: SMTP wenn Email__SmtpHost konfiguriert ist,
    /// sonst den Logging-Fallback (lokale Entwicklung ohne Mail-Zugangsdaten).
    /// </summary>
    public static IServiceCollection AddEmailFeature(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(EmailOptions.SectionName);
        services.Configure<EmailOptions>(section);

        var options = section.Get<EmailOptions>() ?? new EmailOptions();
        if (options.IsConfigured)
        {
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
        }
        else
        {
            services.AddSingleton<IEmailSender, LoggingEmailSender>();
        }

        return services;
    }
}
