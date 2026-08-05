using FluentAssertions;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Heimatplatz.Api.Features.Auth.Services;
using Heimatplatz.Api.Features.Locations.Data.Entities;
using Heimatplatz.Api.Features.Notifications.Contracts.Events;
using Heimatplatz.Api.Features.OpenImmoImport.Configuration;
using Heimatplatz.Api.Features.OpenImmoImport.Models;
using Heimatplatz.Api.Features.OpenImmoImport.Services;
using Heimatplatz.Api.Features.Properties.Contracts;
using Heimatplatz.Api.Features.Properties.Contracts.Enums;
using Heimatplatz.Api.Features.Properties.Contracts.Models.TypeSpecific;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Heimatplatz.Api.Features.Properties.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using Shiny.Mediator;

namespace Heimatplatz.Api.Core.UnitTests.Features.OpenImmoImport;

[TestFixture]
public class OpenImmoPropertySyncServiceTests
{
    private SqliteConnection _connection = null!;
    private AppDbContext _dbContext = null!;
    private IPasswordHasher _passwordHasher = null!;
    private ISellerInfoResolver _sellerInfoResolver = null!;
    private IOpenImmoImageService _imageService = null!;
    private IPropertyGeocoder _geocoder = null!;
    private IMediator _mediator = null!;
    private OpenImmoImportOptions _options = null!;
    private OpenImmoFeedOptions _feed = null!;
    private Guid _municipalityWelsId;

    [SetUp]
    public void SetUp()
    {
        // Feature-Assemblies VOR dem Modellbau laden (Entity-Auto-Discovery scannt
        // nur bereits geladene Heimatplatz-Assemblies)
        _ = typeof(Property).Assembly;
        _ = typeof(User).Assembly;
        _ = typeof(Municipality).Assembly;

        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var contextOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        _dbContext = new AppDbContext(contextOptions);
        _dbContext.Database.EnsureCreated();

        SeedMunicipalities();

        // Echte SellerSource-Zeile: der Resolver-Substitute liefert nur die Id,
        // die FK-Constraint braucht aber eine existierende Zeile
        var sellerSource = new SellerSource
        {
            Id = Guid.Parse("11111111-2222-3333-4444-555555555555"),
            Name = "Immobär Immobilien"
        };
        _dbContext.Set<SellerSource>().Add(sellerSource);
        _dbContext.SaveChanges();
        _dbContext.ChangeTracker.Clear();

        _passwordHasher = Substitute.For<IPasswordHasher>();
        _passwordHasher.Hash(Arg.Any<string>()).Returns("hashed");

        _sellerInfoResolver = Substitute.For<ISellerInfoResolver>();
        _sellerInfoResolver
            .ResolveSellerSourceAsync(Arg.Any<SellerType>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(sellerSource.Id);

        _imageService = Substitute.For<IOpenImmoImageService>();
        _imageService
            .MaterializeAsync(Arg.Any<OpenImmoFeedOptions>(), Arg.Any<OpenImmoListing>(),
                Arg.Any<IOpenImmoZipAccessor?>(), Arg.Any<CancellationToken>())
            .Returns([]);

        _geocoder = Substitute.For<IPropertyGeocoder>();
        _geocoder
            .GeocodeAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((PropertyGeocodeResult?)null);

        _mediator = Substitute.For<IMediator>();

        _options = new OpenImmoImportOptions();
        _feed = new OpenImmoFeedOptions
        {
            Key = "immobaer",
            SourceName = "immobaer.at",
            SellerName = "Immobär Immobilien"
        };
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    private void SeedMunicipalities()
    {
        var province = new FederalProvince { Id = Guid.NewGuid(), Key = "4", Name = "Oberösterreich" };
        var district = new District
        {
            Id = Guid.NewGuid(), Key = "403", Code = "403", Name = "Wels", FederalProvinceId = province.Id
        };
        var wels = new Municipality
        {
            Id = Guid.NewGuid(), Key = "40301", Code = "40301", Name = "Wels",
            PostalCode = "4600", DistrictId = district.Id
        };
        var marchtrenk = new Municipality
        {
            Id = Guid.NewGuid(), Key = "41013", Code = "41013", Name = "Marchtrenk",
            PostalCode = "4614", DistrictId = district.Id
        };
        _municipalityWelsId = wels.Id;

        _dbContext.Set<FederalProvince>().Add(province);
        _dbContext.Set<District>().Add(district);
        _dbContext.Set<Municipality>().AddRange(wels, marchtrenk);
        _dbContext.SaveChanges();
        _dbContext.ChangeTracker.Clear();
    }

    private OpenImmoPropertySyncService CreateService() => new(
        _dbContext, _passwordHasher, _sellerInfoResolver, _imageService, _geocoder,
        Options.Create(_options), _mediator, NullLogger<OpenImmoPropertySyncService>.Instance);

    private static OpenImmoListing CreateHausListing(string sourceId = "OBID-001") => new()
    {
        SourceId = sourceId,
        Type = PropertyType.House,
        Title = "Traumhaus in Wels",
        Description = "Schönes Haus.",
        Street = "Ringstraße 12",
        AddressReleased = true,
        PostalCode = "4600",
        City = "Wels",
        Price = 520000.50m,
        LivingAreaSquareMeters = 146,
        PlotAreaSquareMeters = 850,
        Rooms = 5,
        YearBuilt = 1998,
        Condition = PropertyCondition.Good,
        Features = ["Keller", "Garage"],
        HasGarage = true,
        HasBasement = true,
        Contact = new OpenImmoContact
        {
            Name = "Max Huber",
            Email = "max.huber@immobaer.at",
            Phone = "+43 660 1234567"
        },
        StandVom = new DateTimeOffset(2026, 7, 30, 0, 0, 0, TimeSpan.Zero)
    };

    private static OpenImmoParseResult FullSnapshot(params OpenImmoListing[] listings) => new()
    {
        Listings = listings.ToList()
    };

    [Test]
    public async Task Sync_NeuesObjekt_LegtPropertyMitAllenFeldernAn()
    {
        var result = await CreateService().SyncAsync(_feed, FullSnapshot(CreateHausListing()), zip: null);

        result.Created.Should().Be(1);
        result.Errors.Should().Be(0);

        var property = await _dbContext.Set<Property>().Include(p => p.Contacts)
            .SingleAsync(p => p.SourceId == "OBID-001");
        property.SourceName.Should().Be("immobaer.at");
        property.Title.Should().Be("Traumhaus in Wels");
        property.Address.Should().Be("Ringstraße 12");
        property.MunicipalityId.Should().Be(_municipalityWelsId);
        property.PostalCode.Should().Be("4600");
        property.Price.Should().Be(520000.50m);
        property.Type.Should().Be(PropertyType.House);
        property.SellerType.Should().Be(SellerType.Broker);
        property.SellerName.Should().Be("Immobär Immobilien");
        property.SellerSourceId.Should().Be(Guid.Parse("11111111-2222-3333-4444-555555555555"));
        property.UserId.Should().Be(OpenImmoImportConstants.SystemUserId);
        property.LocationDisplay.Should().Be(LocationDisplayMode.Exact);
        property.SourceLastUpdated.Should().Be(new DateTimeOffset(2026, 7, 30, 0, 0, 0, TimeSpan.Zero));
        property.Features.Should().BeEquivalentTo("Keller", "Garage");

        var typed = property.GetTypedData<HousePropertyData>();
        typed.Should().NotBeNull();
        typed!.HasBasement.Should().BeTrue();
        typed!.Condition.Should().Be(PropertyCondition.Good);

        property.Contacts.Should().ContainSingle();
        property.Contacts[0].Type.Should().Be(ContactType.Agent);
        property.Contacts[0].Source.Should().Be(ContactSource.Import);
        property.Contacts[0].Name.Should().Be("Max Huber");

        await _mediator.Received(1).Publish(
            Arg.Is<PropertyCreatedEvent>(e => e != null && e.Title == "Traumhaus in Wels" && e.City == "Wels"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Sync_UnveraenderterLauf_FasstDieZeileNichtAn()
    {
        var listing = CreateHausListing();
        await CreateService().SyncAsync(_feed, FullSnapshot(listing), zip: null);
        _dbContext.ChangeTracker.Clear();

        var updatedAtBefore = (await _dbContext.Set<Property>().AsNoTracking()
            .SingleAsync(p => p.SourceId == "OBID-001")).UpdatedAt;

        var result = await CreateService().SyncAsync(_feed, FullSnapshot(listing), zip: null);

        result.Created.Should().Be(0);
        result.Updated.Should().Be(0);
        result.Unchanged.Should().Be(1);

        _dbContext.ChangeTracker.Clear();
        var updatedAtAfter = (await _dbContext.Set<Property>().AsNoTracking()
            .SingleAsync(p => p.SourceId == "OBID-001")).UpdatedAt;
        updatedAtAfter.Should().Be(updatedAtBefore, "No-Op-Laeufe duerfen kein Delta-Journal-Rauschen erzeugen");
    }

    [Test]
    public async Task Sync_Preisaenderung_Aktualisiert()
    {
        var listing = CreateHausListing();
        await CreateService().SyncAsync(_feed, FullSnapshot(listing), zip: null);
        _dbContext.ChangeTracker.Clear();

        var changed = listing with { Price = 499000m };
        var result = await CreateService().SyncAsync(_feed, FullSnapshot(changed), zip: null);

        result.Updated.Should().Be(1);
        result.Unchanged.Should().Be(0);

        _dbContext.ChangeTracker.Clear();
        var property = await _dbContext.Set<Property>().SingleAsync(p => p.SourceId == "OBID-001");
        property.Price.Should().Be(499000m);
    }

    [Test]
    public async Task Sync_Neubauprojekt_FlagWirdGespeichertUndBeimUpdateZurueckgesetzt()
    {
        var neubau = CreateHausListing() with { IsNewBuildProject = true, YearBuilt = null };
        await CreateService().SyncAsync(_feed, FullSnapshot(neubau), zip: null);
        _dbContext.ChangeTracker.Clear();

        var created = await _dbContext.Set<Property>().AsNoTracking().SingleAsync(p => p.SourceId == "OBID-001");
        created.IsNewBuildProject.Should().BeTrue();

        // Haus fertiggestellt: Feed liefert das Objekt spaeter ohne Neubau-Merkmale
        var fertig = neubau with { IsNewBuildProject = false, YearBuilt = 2026 };
        var result = await CreateService().SyncAsync(_feed, FullSnapshot(fertig), zip: null);

        result.Updated.Should().Be(1);

        _dbContext.ChangeTracker.Clear();
        var property = await _dbContext.Set<Property>().AsNoTracking().SingleAsync(p => p.SourceId == "OBID-001");
        property.IsNewBuildProject.Should().BeFalse();
    }

    [Test]
    public async Task Sync_VerschwundenesObjekt_WirdGeloeschtInklBilder()
    {
        await CreateService().SyncAsync(
            _feed, FullSnapshot(CreateHausListing("OBID-001"), CreateHausListing("OBID-002")), zip: null);
        _dbContext.ChangeTracker.Clear();

        var result = await CreateService().SyncAsync(
            _feed, FullSnapshot(CreateHausListing("OBID-001")), zip: null);

        result.Removed.Should().Be(1);
        _dbContext.ChangeTracker.Clear();
        (await _dbContext.Set<Property>().CountAsync(p => p.SourceName == "immobaer.at")).Should().Be(1);
        _imageService.Received(1).DeleteListingImages("immobaer", "OBID-002");
    }

    [Test]
    public async Task Sync_AusgeblendetesObjekt_UeberlebtDenDeletePass()
    {
        await CreateService().SyncAsync(_feed, FullSnapshot(CreateHausListing("OBID-001")), zip: null);
        _dbContext.ChangeTracker.Clear();

        var property = await _dbContext.Set<Property>().SingleAsync(p => p.SourceId == "OBID-001");
        property.IsHidden = true;
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var result = await CreateService().SyncAsync(
            _feed, FullSnapshot(CreateHausListing("OBID-999")), zip: null);

        result.Removed.Should().Be(0, "Moderation ueberlebt einen Delete+Recreate-Zyklus");
        _dbContext.ChangeTracker.Clear();
        (await _dbContext.Set<Property>().AnyAsync(p => p.SourceId == "OBID-001" && p.IsHidden))
            .Should().BeTrue();
    }

    [Test]
    public async Task Sync_MunicipalityMiss_UeberspringtOhneBestandZuLoeschen()
    {
        await CreateService().SyncAsync(_feed, FullSnapshot(CreateHausListing("OBID-001")), zip: null);
        _dbContext.ChangeTracker.Clear();

        // Gleiches Objekt, aber PLZ/Ort nicht aufloesbar (z.B. Datenfehler im Feed)
        var unresolvable = CreateHausListing("OBID-001") with { PostalCode = "9999", City = "Nirgendwo" };
        var result = await CreateService().SyncAsync(_feed, FullSnapshot(unresolvable), zip: null);

        result.Skipped.Should().Be(1);
        result.Removed.Should().Be(0,
            "ein transient nicht aufloesbares Objekt darf den Bestand nicht loeschen (SourceId-Registrierung VOR der Aufloesung)");
        _dbContext.ChangeTracker.Clear();
        (await _dbContext.Set<Property>().AnyAsync(p => p.SourceId == "OBID-001")).Should().BeTrue();
    }

    [Test]
    public async Task Sync_LeererVollbestand_BrichtAbStattZuLoeschen()
    {
        await CreateService().SyncAsync(_feed, FullSnapshot(CreateHausListing("OBID-001")), zip: null);
        _dbContext.ChangeTracker.Clear();

        var result = await CreateService().SyncAsync(_feed, FullSnapshot(), zip: null);

        result.Aborted.Should().BeTrue();
        result.Errors.Should().Be(1);
        _dbContext.ChangeTracker.Clear();
        (await _dbContext.Set<Property>().AnyAsync(p => p.SourceId == "OBID-001")).Should().BeTrue();
    }

    [Test]
    public async Task Sync_TeilUebertragung_LoeschtNurExpliziteDeletes()
    {
        await CreateService().SyncAsync(
            _feed, FullSnapshot(CreateHausListing("OBID-001"), CreateHausListing("OBID-002")), zip: null);
        _dbContext.ChangeTracker.Clear();

        var partial = new OpenImmoParseResult
        {
            Listings = [],
            IsPartialTransfer = true,
            DeletedSourceIds = ["OBID-002"]
        };
        var result = await CreateService().SyncAsync(_feed, partial, zip: null);

        result.Removed.Should().Be(1);
        _dbContext.ChangeTracker.Clear();
        (await _dbContext.Set<Property>().AnyAsync(p => p.SourceId == "OBID-001"))
            .Should().BeTrue("TEIL-Uebertragungen loeschen nicht per Snapshot-Diff");
        (await _dbContext.Set<Property>().AnyAsync(p => p.SourceId == "OBID-002")).Should().BeFalse();
    }

    [Test]
    public async Task Sync_FeedKoordinaten_WerdenOhneGeocoderUebernommen()
    {
        var listing = CreateHausListing() with { Latitude = 48.16123, Longitude = 14.03456 };

        await CreateService().SyncAsync(_feed, FullSnapshot(listing), zip: null);

        _dbContext.ChangeTracker.Clear();
        var property = await _dbContext.Set<Property>().SingleAsync(p => p.SourceId == "OBID-001");
        property.Latitude.Should().BeApproximately(48.16123, 0.00001);
        property.IsLocationExact.Should().BeTrue();
        await _geocoder.DidNotReceive().GeocodeAsync(
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Sync_OhneKoordinaten_NutztGeocoderFallback()
    {
        _geocoder
            .GeocodeAsync("Ringstraße 12", "4600", "Wels", Arg.Any<CancellationToken>())
            .Returns(new PropertyGeocodeResult(48.1, 14.0, IsExact: true));

        await CreateService().SyncAsync(_feed, FullSnapshot(CreateHausListing()), zip: null);

        _dbContext.ChangeTracker.Clear();
        var property = await _dbContext.Set<Property>().SingleAsync(p => p.SourceId == "OBID-001");
        property.Latitude.Should().BeApproximately(48.1, 0.001);
        property.IsLocationExact.Should().BeTrue();
    }

    [Test]
    public async Task Sync_ExternalUrl_LandetInSourceUrlUndKontakt()
    {
        var listing = CreateHausListing() with { ExternalUrl = "https://apps.justimmo.at/website/objekt/OBID-001" };
        await CreateService().SyncAsync(_feed, FullSnapshot(listing), zip: null);
        _dbContext.ChangeTracker.Clear();

        var property = await _dbContext.Set<Property>().Include(p => p.Contacts)
            .SingleAsync(p => p.SourceId == "OBID-001");
        property.SourceUrl.Should().Be("https://apps.justimmo.at/website/objekt/OBID-001");
        property.Contacts.Single().OriginalListingUrl.Should().Be("https://apps.justimmo.at/website/objekt/OBID-001");
    }

    [Test]
    public async Task Sync_NachtraeglicheUrl_AktualisiertBestandskontakt()
    {
        // Bestand ohne URL (wie der Erst-Import vor dem url-Feld-Support)
        var ohneUrl = CreateHausListing();
        await CreateService().SyncAsync(_feed, FullSnapshot(ohneUrl), zip: null);
        _dbContext.ChangeTracker.Clear();

        var mitUrl = ohneUrl with { ExternalUrl = "https://apps.justimmo.at/website/objekt/OBID-001" };
        var result = await CreateService().SyncAsync(_feed, FullSnapshot(mitUrl), zip: null);

        result.Updated.Should().Be(1, "die neue URL muss als Aenderung erkannt werden");
        _dbContext.ChangeTracker.Clear();
        var property = await _dbContext.Set<Property>().Include(p => p.Contacts)
            .SingleAsync(p => p.SourceId == "OBID-001");
        property.Contacts.Single().OriginalListingUrl.Should().NotBeNull();
    }

    [Test]
    public async Task Sync_AdresseNichtFreigegeben_BleibtApproximateOhneStrasse()
    {
        var listing = CreateHausListing() with { AddressReleased = false };

        await CreateService().SyncAsync(_feed, FullSnapshot(listing), zip: null);

        _dbContext.ChangeTracker.Clear();
        var property = await _dbContext.Set<Property>().SingleAsync(p => p.SourceId == "OBID-001");
        property.Address.Should().BeEmpty();
        property.LocationDisplay.Should().Be(LocationDisplayMode.Approximate);
    }
}
