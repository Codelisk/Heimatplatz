using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.Api.Core.Data.Configuration;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Name der separaten Migrations-Assembly fuer Postgres (siehe
    /// Heimatplatz.Api.Core.Data.Migrations.Postgres). Postgres braucht eine eigene Migrations-
    /// Historie, weil EF Core Migrationen pro (Assembly, DbContext-Typ) auflistet - SqlServer/
    /// Sqlite teilen sich weiterhin die Migrations/ in diesem Projekt.
    /// </summary>
    private const string PostgresMigrationsAssembly = "Heimatplatz.Api.Core.Data.Migrations.Postgres";

    public static IServiceCollection AddAppData(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // DatabaseOptions konfigurieren
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        var provider = configuration[$"{DatabaseOptions.SectionName}:Provider"];

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            // Features koennen SaveChanges-Interceptors beisteuern (als IInterceptor registrieren),
            // z.B. das PropertyChange-Journal fuer den Client-Delta-Sync
            options.AddInterceptors(serviceProvider.GetServices<IInterceptor>());

            // Für Build-Zeit Tools (OpenAPI Generator): InMemory Provider verwenden
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseInMemoryDatabase("BuildTimeDb");
            }
            else if (string.Equals(provider, "Postgres", StringComparison.OrdinalIgnoreCase))
            {
                options.UseNpgsql(connectionString, x => x.MigrationsAssembly(PostgresMigrationsAssembly));
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
