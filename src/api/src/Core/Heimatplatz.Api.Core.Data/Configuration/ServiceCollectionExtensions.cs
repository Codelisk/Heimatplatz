using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.Api.Core.Data.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAppData(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // DatabaseOptions konfigurieren
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));

        services.AddDbContext<AppDbContext>(options =>
        {
            // Für Build-Zeit Tools (OpenAPI Generator): InMemory Provider verwenden
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseInMemoryDatabase("BuildTimeDb");
            }
            else if (connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlite(connectionString);
            }
            else
            {
                options.UseSqlServer(connectionString);
            }

            // PendingModelChangesWarning unterdruecken: die FixProductionSchemaDrift-Migration
            // hat absichtlich keinen aktualisierten Model-Snapshot, weil sie keine Entity-Aenderungen
            // vornimmt (nur idempotenten Schema-Sync per Raw-SQL).
            options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        return services;
    }
}
