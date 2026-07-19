using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using System.Text.RegularExpressions;
using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Exceptions;
using Heimatplatz.Api.Features.Telemetry.Configuration;
using Heimatplatz.Api.Features.Telemetry.Contracts.Mediator.Models;
using Heimatplatz.Api.Features.Telemetry.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Telemetry.Data.Entities;
using Heimatplatz.Api.Features.Telemetry.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Telemetry.Handlers;

/// <summary>
/// Anonymer Ingestion-Endpoint fuer Client-Fehler (MAUI-Crash-Reports, Client-Logs).
/// Schreibt direkt ueber den AppDbContext (unabhaengig von der OTel-Pipeline, damit
/// auch InMemory-Tests funktionieren). Missbrauchsschutz: Rate-Limit 20/min/IP
/// (Program.cs) + harte Caps auf Batch-Groesse und Stringlaengen.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/telemetry")]
public partial class IngestClientLogsHandler(
    AppDbContext dbContext,
    ErrorFingerprintService fingerprintService,
    ErrorGroupUpserter errorGroupUpserter,
    IHttpContextAccessor httpContextAccessor,
    IOptions<TelemetryOptions> options
) : IRequestHandler<IngestClientLogsRequest, IngestClientLogsResponse>
{
    [GeneratedRegex("^00-([0-9a-f]{32})-([0-9a-f]{16})-[0-9a-f]{2}$")]
    private static partial Regex TraceparentRegex();

    [MediatorHttpPost("/client-logs", OperationId = "IngestClientLogs")]
    public async Task<IngestClientLogsResponse> Handle(IngestClientLogsRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        var caps = options.Value.ClientIngestion;

        if (request.Source == TelemetrySource.Api)
            throw new ValidationException("Source muss Maui oder Web sein");
        if (request.Entries is not { Count: > 0 })
            throw new ValidationException("Entries darf nicht leer sein");
        if (request.Entries.Count > caps.MaxBatchEntries)
            throw new ValidationException($"Maximal {caps.MaxBatchEntries} Eintraege pro Request");

        var now = DateTimeOffset.UtcNow;
        // Endpoint ist anonym, aber wenn ein gueltiges JWT mitkommt, den User zuordnen
        var userId = httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var clientApp = Truncate($"{request.Source}/{request.AppVersion ?? "?"}", 128);
        var attributesJson = JsonSerializer.Serialize(new Dictionary<string, string?>
        {
            ["appVersion"] = request.AppVersion,
            ["platform"] = request.Platform
        });

        var logs = new List<TelemetryLog>(request.Entries.Count);
        var fingerprinted = new List<(TelemetryLog Log, ErrorFingerprint Fingerprint)>();

        foreach (var entry in request.Entries)
        {
            var log = new TelemetryLog
            {
                Id = Guid.CreateVersion7(),
                // Zukunfts-Zeitstempel (Client-Uhr) auf Serverzeit klemmen
                TimestampUtc = entry.TimestampUtc > now.AddMinutes(5) ? now : entry.TimestampUtc,
                Level = TelemetryQueryHelpers.RankOfLevel(entry.Level) >= 0
                    ? entry.Level
                    : (entry.ExceptionText != null ? "Error" : "Information"),
                Category = "Client",
                Message = Truncate(entry.Message, caps.MaxMessageLength) ?? "",
                UserId = Truncate(userId, 64),
                ClientApp = clientApp,
                Source = request.Source,
                AttributesJson = attributesJson
            };

            if (entry.Screen != null || entry.Traceparent != null)
            {
                if (entry.Traceparent != null && TraceparentRegex().Match(entry.Traceparent) is { Success: true } match)
                {
                    log.TraceId = match.Groups[1].Value;
                    log.SpanId = match.Groups[2].Value;
                }

                if (entry.Screen != null)
                {
                    log.AttributesJson = JsonSerializer.Serialize(new Dictionary<string, string?>
                    {
                        ["appVersion"] = request.AppVersion,
                        ["platform"] = request.Platform,
                        ["screen"] = Truncate(entry.Screen, 256)
                    });
                }
            }

            if (!string.IsNullOrWhiteSpace(entry.ExceptionText))
            {
                var exceptionText = Truncate(entry.ExceptionText, caps.MaxStackTraceLength)!;
                var (type, message, stackTrace) = fingerprintService.ParseExceptionText(exceptionText);

                log.ExceptionType = Truncate(type, 512);
                log.ExceptionMessage = message;
                log.ExceptionStackTrace = exceptionText;

                fingerprinted.Add((log, new ErrorFingerprint(
                    Hash: fingerprintService.Fingerprint(type, stackTrace, null),
                    ExceptionType: type,
                    Title: fingerprintService.BuildTitle(type, message),
                    SampleMessage: message,
                    SampleStackTrace: exceptionText)));
            }

            logs.Add(log);
        }

        try
        {
            await PersistAsync(logs, fingerprinted, cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Unique-Index-Rennen mit dem TelemetryWriter beim Anlegen derselben
            // Fehlergruppe: einmal neu aufsetzen und wiederholen
            dbContext.ChangeTracker.Clear();
            await PersistAsync(logs, fingerprinted, cancellationToken);
        }

        return new IngestClientLogsResponse(logs.Count);
    }

    private async Task PersistAsync(
        List<TelemetryLog> logs,
        List<(TelemetryLog Log, ErrorFingerprint Fingerprint)> fingerprinted,
        CancellationToken cancellationToken)
    {
        await errorGroupUpserter.ApplyAsync(dbContext, fingerprinted, cancellationToken);
        dbContext.AddRange(logs);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string? Truncate(string? value, int maxLength)
        => value == null || value.Length <= maxLength ? value : value[..maxLength];
}
