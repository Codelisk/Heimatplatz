using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Heimatplatz.Api.IntegrationTests.Infrastructure;

/// <summary>
/// API-Testhost mit einer eigenen temporaeren SQLite-Datei.
/// Damit lassen sich Provider-spezifische Fehler testen, die EF InMemory nicht abbildet.
/// </summary>
public sealed class SqliteWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>
    where TProgram : class
{
    private readonly string databasePath = Path.Combine(
        Path.GetTempPath(),
        $"heimatplatz-sqlite-integration-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting(
            "ConnectionStrings:DefaultConnection",
            $"Data Source={databasePath};Pooling=False");
        builder.UseSetting("Database:Provider", string.Empty);
        builder.UseSetting("Database:AutoMigrate", "true");
        builder.UseSetting("Database:EnableSeeding", "false");
        builder.UseSetting("Database:ForceRecreate", "false");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }
    }
}
