using System.Reflection;
using Heimatplatz.Api.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Heimatplatz.Api.Core.Data.Migrations.Postgres;

/// <summary>
/// Design-Time-Factory fuer "dotnet ef migrations add" gegen Postgres. Erzeugt eine eigene
/// Migrations-Historie fuer AppDbContext (siehe MigrationsAssembly-Override in
/// Heimatplatz.Api.Core.Data/Configuration/ServiceCollectionExtensions.cs), weil EF Core
/// Migrationen pro (Assembly, DbContext-Typ) auflistet - Postgres darf sich die Migrations/ von
/// SQLite/SQL Server nicht teilen, sonst wuerden inkompatible Migrationen gegenseitig als
/// "pending" erscheinen. Der Connection-String ist rein syntaktisch, es wird beim Scaffolding
/// nie tatsaechlich verbunden.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // AppDbContext.OnModelCreating registriert Entity-Configurations per Reflection ueber
        // AppDomain.CurrentDomain.GetAssemblies() - referenzierte Assemblies werden vom CLR aber
        // nur bei tatsaechlicher Nutzung geladen (lazy loading), nicht allein durch eine
        // ProjectReference. Alle Heimatplatz.*.dll im Output-Ordner explizit laden, damit die
        // Feature-Entities (Properties, Users, ForeclosureAuctions, ...) im gescaffoldeten Modell
        // landen.
        foreach (var dll in Directory.GetFiles(AppContext.BaseDirectory, "Heimatplatz.Api.*.dll"))
        {
            try
            {
                Assembly.LoadFrom(dll);
            }
            catch
            {
                // Bereits geladen oder keine verwaltete Assembly - ignorieren
            }
        }

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=heimatplatz;Username=postgres;Password=postgres",
            x => x.MigrationsAssembly("Heimatplatz.Api.Core.Data.Migrations.Postgres"));
        return new AppDbContext(optionsBuilder.Options);
    }
}
