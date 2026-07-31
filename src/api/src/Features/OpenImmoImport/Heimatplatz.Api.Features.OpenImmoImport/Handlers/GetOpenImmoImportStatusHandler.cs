using Heimatplatz.Api;
using Heimatplatz.Api.Authorization;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.OpenImmoImport.Configuration;
using Heimatplatz.Api.Features.OpenImmoImport.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.OpenImmoImport.Services;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.OpenImmoImport.Handlers;

[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/openimmo-import")]
public class GetOpenImmoImportStatusHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor,
    IOptions<OpenImmoImportOptions> options,
    IHostEnvironment environment
) : IRequestHandler<GetOpenImmoImportStatusRequest, GetOpenImmoImportStatusResponse>
{
    // Gleicher Shared-Key wie der Trigger: der Status nennt Dateinamen und
    // Bestandszahlen und ist rein fuer den Intern-Bereich gedacht (anders als der
    // oeffentliche ZV-Status).
    [MediatorHttpGet("/status", OperationId = "GetOpenImmoImportStatus")]
    public async Task<GetOpenImmoImportStatusResponse> Handle(
        GetOpenImmoImportStatusRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var providedKey = httpContextAccessor.HttpContext?.Request
            .Headers[TriggerOpenImmoImportHandler.TriggerKeyHeader].ToString();
        if (!SharedKeyAuthorization.IsAuthorized(
                options.Value.SyncTriggerKey, providedKey, environment.IsDevelopment()))
        {
            throw new UnauthorizedAccessException("Sync-Trigger-Key fehlt oder ist ungueltig.");
        }

        var feeds = new List<OpenImmoFeedStatus>();
        var configured = !string.IsNullOrWhiteSpace(options.Value.IncomingRootPath);
        var stateRoot = configured ? options.Value.ResolveStateRootPath() : null;

        foreach (var feed in options.Value.Feeds)
        {
            var marker = stateRoot != null
                ? await OpenImmoMarkerStore.ReadAsync(stateRoot, feed.Key, cancellationToken)
                : null;

            var propertyCount = await dbContext.Set<Property>()
                .CountAsync(p => p.SourceName == feed.SourceName, cancellationToken);

            feeds.Add(new OpenImmoFeedStatus
            {
                FeedKey = feed.Key,
                SourceName = feed.SourceName,
                LastImportAt = marker?.ImportedAtUtc,
                LastFileName = marker?.FileName,
                LastFileWriteTime = marker?.LastWriteTimeUtc,
                LastResultSummary = marker?.Summary,
                PropertyCount = propertyCount
            });
        }

        return new GetOpenImmoImportStatusResponse
        {
            IsRunning = OpenImmoImportGuard.IsRunning,
            Feeds = feeds
        };
    }
}
