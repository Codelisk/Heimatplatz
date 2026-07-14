using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Heimatplatz.Api.Core.Data;

/// <summary>
/// Design-Time-Factory fuer "dotnet ef migrations add" gegen SQLite/SQL Server (die Standard-
/// Migrations-Assembly dieses Projekts). Ohne diese Factory wuerde die EF-Tooling mangels
/// IDesignTimeDbContextFactory auf Program.cs zurueckfallen und dabei ungewollt den gesamten
/// Host inkl. InitializeDatabaseAsync() (Migration + Seeding) gegen die lokale Dev-DB ausfuehren.
/// Die Connection-String-Form ist rein syntaktisch - fuer das Scaffolding wird nie verbunden.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Referenced feature assemblies are loaded lazily. Load the copies present in the
        // startup output so OnModelCreating can discover every feature configuration.
        foreach (var dll in Directory.GetFiles(AppContext.BaseDirectory, "Heimatplatz.Api.*.dll"))
        {
            try
            {
                Assembly.LoadFrom(dll);
            }
            catch
            {
                // Already loaded or not a managed assembly.
            }
        }

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        if (args.Any(x => string.Equals(x, "Postgres", StringComparison.OrdinalIgnoreCase)))
        {
            optionsBuilder.UseNpgsql(
                "Host=localhost;Database=heimatplatz;Username=postgres;Password=postgres",
                x => x.MigrationsAssembly("Heimatplatz.Api.Core.Data.Migrations.Postgres"));
        }
        else
        {
            optionsBuilder.UseSqlite("Data Source=design-time.db");
        }
        return new AppDbContext(optionsBuilder.Options);
    }
}
