using Heimatplatz.Api.Core.Data.Configuration;
using Heimatplatz.Api.Core.Data.Seeding;
using Heimatplatz.Api.Core.Data.Seeding.Configuration;
using Heimatplatz.Api.Features.Immobilien.Configuration;
using Heimatplatz.Api.Features.Immobilien.Contracts.Mediator.Requests;
using Microsoft.Extensions.Configuration;
using Shiny.Mediator;

namespace Heimatplatz.Api.Core.Startup;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Data (must be before features to register DbContext first)
        services.AddAppData(configuration);
        services.AddDataSeeding();

        services.AddShinyServiceRegistry();
        services.AddShinyMediator();

        // Features
        services.AddImmobilienFeature();

        return services;
    }

    public static WebApplication MapEndpoints(this WebApplication app)
    {
        // Immobilien Feature Endpoints
        var immobilienGroup = app.MapGroup("/api/immobilien");

        immobilienGroup.MapMediatorGet<GetImmobilienRequest, GetImmobilienResponse>("");
        immobilienGroup.MapMediatorGet<GetImmobilieByIdRequest, GetImmobilieByIdResponse>("{id}");
        immobilienGroup.MapMediatorGet<GetImmobilienTypenRequest, GetImmobilienTypenResponse>("typen");
        immobilienGroup.MapMediatorGet<GetImmobilienAnzahlRequest, GetImmobilienAnzahlResponse>("anzahl");

        return app;
    }

    public static async Task RunSeedersAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var seederRunner = scope.ServiceProvider.GetRequiredService<SeederRunner>();
        await seederRunner.RunAllSeedersAsync();
    }
}