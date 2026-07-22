using FluentAssertions;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Heimatplatz.Api.UnitTests.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Heimatplatz.Api.Core.UnitTests.Data;

/// <summary>
/// Tests fuer AppDbContext auf dem SQLite-Provider (lokale Entwicklung).
/// Regressionsschutz fuer die SQLite-Workaround-Konverter: Ohne sie wirft SQLite
/// "SQLite cannot order by expressions of type 'DateTimeOffset'/'decimal'" -
/// die Listen-Endpoints sortieren und paginieren seit Juli 2026 in der Datenbank.
/// </summary>
[TestFixture]
[Category(TestCategories.Core)]
[Category(TestCategories.Data)]
[Category(TestCategories.Unit)]
public class AppDbContextTests : BaseApiUnitTest
{
    private SqliteConnection _connection = null!;
    private AppDbContext _dbContext = null!;

    [SetUp]
    public void SetUpDbContext()
    {
        // Feature-Assemblies VOR dem Modellbau laden: Die Entity-Auto-Discovery im
        // AppDbContext scannt nur bereits geladene Heimatplatz-Assemblies - im
        // Testprozess sind sie sonst beim EnsureCreated noch nicht da.
        _ = typeof(Property).Assembly;
        _ = typeof(User).Assembly;

        // Geteilte In-Memory-SQLite-DB: lebt solange die Verbindung offen bleibt
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new AppDbContext(options);
        _dbContext.Database.EnsureCreated();
    }

    [TearDown]
    public void TearDownDbContext()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    [Test]
    [Category(TestCategories.Smoke)]
    public void Sqlite_OrderByDateTimeOffsetAndDecimal_IsTranslatable()
    {
        var byCreatedAt = () => _dbContext.Set<Property>().OrderByDescending(p => p.CreatedAt).ToList();
        var byPrice = () => _dbContext.Set<Property>().OrderBy(p => p.Price).ToList();

        byCreatedAt.Should().NotThrow();
        byPrice.Should().NotThrow();
    }

    [Test]
    public void Sqlite_DateTimeOffset_RoundtripsAndOrdersChronologically()
    {
        var older = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var newer = new DateTimeOffset(2026, 6, 15, 8, 30, 0, TimeSpan.Zero);

        _dbContext.Set<User>().AddRange(
            CreateUser("alt@test.dev", older),
            CreateUser("neu@test.dev", newer));
        _dbContext.SaveChanges();
        _dbContext.ChangeTracker.Clear();

        var result = _dbContext.Set<User>()
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new { u.Email, u.CreatedAt })
            .ToList();

        result.Should().HaveCount(2);
        result[0].Email.Should().Be("neu@test.dev");
        result[0].CreatedAt.Should().Be(newer);
        result[1].CreatedAt.Should().Be(older);
    }

    [Test]
    public void SaveChanges_NormalizesPersistedDateTimeOffsetsToUtc()
    {
        var viennaTime = new DateTimeOffset(2026, 7, 22, 8, 30, 0, TimeSpan.FromHours(2));
        var user = CreateUser("utc@test.dev", viennaTime);
        user.EmailVerifiedAt = viennaTime.AddMinutes(15);

        _dbContext.Set<User>().Add(user);
        _dbContext.SaveChanges();

        user.CreatedAt.Offset.Should().Be(TimeSpan.Zero);
        user.CreatedAt.UtcTicks.Should().Be(viennaTime.UtcTicks);
        user.EmailVerifiedAt.Should().NotBeNull();
        user.EmailVerifiedAt!.Value.Offset.Should().Be(TimeSpan.Zero);
        user.EmailVerifiedAt.Value.UtcTicks.Should().Be(viennaTime.AddMinutes(15).UtcTicks);
    }

    private static User CreateUser(string email, DateTimeOffset createdAt) => new()
    {
        Id = Guid.NewGuid(),
        FirstName = "Test",
        LastName = "User",
        Email = email,
        PasswordHash = "hash",
        CreatedAt = createdAt
    };
}
