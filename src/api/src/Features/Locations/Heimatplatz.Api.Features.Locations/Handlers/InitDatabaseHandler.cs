using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Locations.Contracts.Mediator.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Locations.Handlers;

[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/db")]
public class InitDatabaseHandler(
    AppDbContext dbContext,
    ILogger<InitDatabaseHandler> logger
) : IRequestHandler<InitDatabaseRequest, InitDatabaseResponse>
{
    [MediatorHttpPost("/init", OperationId = "InitDatabase")]
    public async Task<InitDatabaseResponse> Handle(
        InitDatabaseRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            // Try to check if tables exist
            try
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "SELECT TOP 1 1 FROM [Properties]", cancellationToken);
                return new InitDatabaseResponse(true, "Tables already exist");
            }
            catch
            {
                // Tables don't exist
            }

            // Try CreateTablesAsync first
            try
            {
                var creator = dbContext.Database.GetService<IRelationalDatabaseCreator>();
                await creator.CreateTablesAsync(cancellationToken);
                return new InitDatabaseResponse(true, "Tables created with CreateTablesAsync");
            }
            catch (Exception ex1)
            {
                logger.LogWarning(ex1, "CreateTablesAsync failed, trying EnsureDeleted+EnsureCreated");

                // Fallback: full recreate
                try
                {
                    await dbContext.Database.EnsureDeletedAsync(cancellationToken);
                }
                catch (Exception delEx)
                {
                    logger.LogWarning(delEx, "EnsureDeletedAsync failed");
                }

                var created = await dbContext.Database.EnsureCreatedAsync(cancellationToken);
                return new InitDatabaseResponse(true,
                    $"EnsureCreated result: {created}. CreateTables error was: {ex1.Message}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "InitDatabase failed");
            return new InitDatabaseResponse(false, $"{ex.GetType().Name}: {ex.Message}");
        }
    }
}
