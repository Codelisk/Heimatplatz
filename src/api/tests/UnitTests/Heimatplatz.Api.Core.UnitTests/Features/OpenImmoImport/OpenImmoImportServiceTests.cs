using FluentAssertions;
using Heimatplatz.Api.Features.OpenImmoImport.Configuration;
using Heimatplatz.Api.Features.OpenImmoImport.Models;
using Heimatplatz.Api.Features.OpenImmoImport.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace Heimatplatz.Api.Core.UnitTests.Features.OpenImmoImport;

/// <summary>
/// Orchestrator-Tests mit echtem FeedReader/Parser und substituiertem Sync -
/// geprueft wird die Marker-Logik (Kurzschluss, kein Marker bei Fehlern, Force).
/// </summary>
[TestFixture]
public class OpenImmoImportServiceTests
{
    private const string ValidXml = """
        <openimmo>
          <uebertragung umfang="VOLL"/>
          <anbieter>
            <immobilie>
              <objektkategorie>
                <vermarktungsart KAUF="1"/>
                <objektart><haus/></objektart>
              </objektkategorie>
              <geo><plz>4600</plz><ort>Wels</ort></geo>
              <preise><kaufpreis>100000</kaufpreis></preise>
              <verwaltung_techn><openimmo_obid>OBID-1</openimmo_obid></verwaltung_techn>
            </immobilie>
          </anbieter>
        </openimmo>
        """;

    private string _rootDir = null!;
    private string _incomingDir = null!;
    private IOpenImmoPropertySyncService _syncService = null!;
    private OpenImmoImportOptions _options = null!;
    private OpenImmoImportService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _rootDir = Path.Combine(Path.GetTempPath(), $"openimmo-import-{Guid.NewGuid():N}");
        _incomingDir = Path.Combine(_rootDir, "incoming");
        Directory.CreateDirectory(Path.Combine(_incomingDir, "immobaer"));

        _syncService = Substitute.For<IOpenImmoPropertySyncService>();
        _syncService
            .SyncAsync(Arg.Any<OpenImmoFeedOptions>(), Arg.Any<OpenImmoParseResult>(),
                Arg.Any<IOpenImmoZipAccessor?>(), Arg.Any<CancellationToken>())
            .Returns(new OpenImmoSyncResult(1, 0, 0, 0, 0, 0, []));

        _options = new OpenImmoImportOptions
        {
            IncomingRootPath = _incomingDir,
            StateRootPath = Path.Combine(_rootDir, "state"),
            FileStableSeconds = 60,
            Feeds =
            [
                new OpenImmoFeedOptions
                {
                    Key = "immobaer",
                    SourceName = "immobaer.at",
                    SellerName = "Immobär Immobilien"
                }
            ]
        };

        _service = new OpenImmoImportService(
            new OpenImmoFeedReader(),
            new OpenImmoParser(),
            _syncService,
            Options.Create(_options),
            NullLogger<OpenImmoImportService>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_rootDir))
            Directory.Delete(_rootDir, recursive: true);
    }

    private string WriteFeedFile(string content, bool stable = true)
    {
        var path = Path.Combine(_incomingDir, "immobaer", "feed.xml");
        File.WriteAllText(path, content);
        if (stable)
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(-5));
        return path;
    }

    [Test]
    public async Task Run_OhneKonfiguration_IstInaktiv()
    {
        var service = new OpenImmoImportService(
            new OpenImmoFeedReader(), new OpenImmoParser(), _syncService,
            Options.Create(new OpenImmoImportOptions()), NullLogger<OpenImmoImportService>.Instance);

        service.IsEnabled.Should().BeFalse();
        var results = await service.TryRunAllFeedsAsync();
        results.Should().BeEmpty();
    }

    [Test]
    public async Task Run_OhneDatei_LiefertNoFile()
    {
        var results = await _service.TryRunAllFeedsAsync();

        results.Should().ContainSingle().Which.Outcome.Should().Be(OpenImmoFeedRunOutcome.NoFile);
    }

    [Test]
    public async Task Run_FrischeDatei_WartetAufUploadAbschluss()
    {
        WriteFeedFile(ValidXml, stable: false);

        var results = await _service.TryRunAllFeedsAsync();

        results!.Single().Outcome.Should().Be(OpenImmoFeedRunOutcome.NotStable);
        await _syncService.DidNotReceiveWithAnyArgs().SyncAsync(default!, default!, default, default);
    }

    [Test]
    public async Task Run_StabileDatei_ImportiertUndSchreibtMarker()
    {
        WriteFeedFile(ValidXml);

        var results = await _service.TryRunAllFeedsAsync();

        results!.Single().Outcome.Should().Be(OpenImmoFeedRunOutcome.Imported);
        await _syncService.ReceivedWithAnyArgs(1).SyncAsync(default!, default!, default, default);

        var marker = await OpenImmoMarkerStore.ReadAsync(_options.StateRootPath!, "immobaer");
        marker.Should().NotBeNull();
        marker!.FileName.Should().Be("feed.xml");
        marker.Summary.Should().Contain("1 neu");
    }

    [Test]
    public async Task Run_UnveraenderteDatei_WirdNichtErneutImportiert()
    {
        WriteFeedFile(ValidXml);
        await _service.TryRunAllFeedsAsync();
        _syncService.ClearReceivedCalls();

        var results = await _service.TryRunAllFeedsAsync();

        results!.Single().Outcome.Should().Be(OpenImmoFeedRunOutcome.Unchanged);
        await _syncService.DidNotReceiveWithAnyArgs().SyncAsync(default!, default!, default, default);
    }

    [Test]
    public async Task Run_Force_UmgehtDenMarker()
    {
        WriteFeedFile(ValidXml);
        await _service.TryRunAllFeedsAsync();
        _syncService.ClearReceivedCalls();

        var results = await _service.TryRunAllFeedsAsync(force: true);

        results!.Single().Outcome.Should().Be(OpenImmoFeedRunOutcome.Imported);
        await _syncService.ReceivedWithAnyArgs(1).SyncAsync(default!, default!, default, default);
    }

    [Test]
    public async Task Run_KaputtesXml_SchreibtKeinenMarker()
    {
        WriteFeedFile("<openimmo><anbieter>");

        var results = await _service.TryRunAllFeedsAsync();

        results!.Single().Outcome.Should().Be(OpenImmoFeedRunOutcome.Failed);
        var marker = await OpenImmoMarkerStore.ReadAsync(_options.StateRootPath!, "immobaer");
        marker.Should().BeNull("kaputte Dateien muessen beim naechsten Tick erneut versucht werden");
    }

    [Test]
    public async Task Run_AbgebrochenerSync_SchreibtKeinenMarker()
    {
        _syncService
            .SyncAsync(Arg.Any<OpenImmoFeedOptions>(), Arg.Any<OpenImmoParseResult>(),
                Arg.Any<IOpenImmoZipAccessor?>(), Arg.Any<CancellationToken>())
            .Returns(new OpenImmoSyncResult(0, 0, 0, 0, 0, 1, ["leer"]) { Aborted = true });
        WriteFeedFile(ValidXml);

        var results = await _service.TryRunAllFeedsAsync();

        results!.Single().Outcome.Should().Be(OpenImmoFeedRunOutcome.Failed);
        (await OpenImmoMarkerStore.ReadAsync(_options.StateRootPath!, "immobaer")).Should().BeNull();
    }

    [Test]
    public async Task Run_GeaenderteDatei_ImportiertErneut()
    {
        WriteFeedFile(ValidXml);
        await _service.TryRunAllFeedsAsync();
        _syncService.ClearReceivedCalls();

        WriteFeedFile(ValidXml.Replace("OBID-1", "OBID-2"));

        var results = await _service.TryRunAllFeedsAsync();

        results!.Single().Outcome.Should().Be(OpenImmoFeedRunOutcome.Imported);
        await _syncService.ReceivedWithAnyArgs(1).SyncAsync(default!, default!, default, default);
    }
}
