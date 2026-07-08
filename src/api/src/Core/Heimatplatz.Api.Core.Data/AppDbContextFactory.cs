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
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlite("Data Source=design-time.db");
        return new AppDbContext(optionsBuilder.Options);
    }
}
