using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory fuer API Integration-Tests.
/// Ersetzt die Datenbank durch eine InMemory-Variante.
/// </summary>
/// <typeparam name="TProgram">Der Program-Typ der API.</typeparam>
public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>
    where TProgram : class
{
    /// <summary>
    /// Konfiguriert den WebHost fuer Tests.
    /// </summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Entferne die echte Datenbank-Registrierung
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<>));

            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            // TODO: Fuege InMemory-DbContext hinzu nach DbContext-Implementierung
            // services.AddDbContext<AppDbContext>(options =>
            // {
            //     options.UseInMemoryDatabase("TestDb");
            // });

            ConfigureTestServices(services);
        });

        builder.UseEnvironment("Testing");
    }

    /// <summary>
    /// Kann ueberschrieben werden um zusaetzliche Test-Services zu konfigurieren.
    /// </summary>
    protected virtual void ConfigureTestServices(IServiceCollection services)
    {
    }
}
