using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Heimatplatz.Api.Core.Data.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAppData(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // Fallback nur für Build-Zeit Tools (OpenAPI Generator etc.)
        // In Production wird die Validierung beim ersten DB-Zugriff fehlschlagen
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Data Source=:memory:";
        }

        // DatabaseOptions konfigurieren
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlite(connectionString);
        });

        return services;
    }

    /// <summary>
    /// Initialisiert die Datenbank basierend auf den DatabaseOptions.
    /// Sollte nach app.Build() aufgerufen werden.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        var options = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;

        if (options.AutoMigrate)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.MigrateAsync();
        }
    }
}
