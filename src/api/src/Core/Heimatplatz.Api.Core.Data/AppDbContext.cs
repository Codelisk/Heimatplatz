using System.Reflection;
using Heimatplatz.Api.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TickerQ.EntityFrameworkCore.Configurations;
using TickerQ.Utilities.Entities;

namespace Heimatplatz.Api.Core.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Auto-scan all loaded assemblies for IEntityTypeConfiguration implementations
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.FullName?.StartsWith("Heimatplatz") == true);

        foreach (var assembly in assemblies)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(assembly);
        }

        // Auto-register entities inheriting from BaseEntity (for entities without explicit configuration)
        foreach (var assembly in assemblies)
        {
            var entityTypes = assembly.GetTypes()
                .Where(t => t is { IsClass: true, IsAbstract: false } && t.IsSubclassOf(typeof(BaseEntity)));

            foreach (var entityType in entityTypes)
            {
                if (modelBuilder.Model.FindEntityType(entityType) == null)
                {
                    modelBuilder.Entity(entityType);
                }
            }
        }

        // TickerQ-Betriebstabellen (Hintergrund-Jobs, z.B. KI-Beschreibungs-Generierung der
        // Inserat-Entwuerfe). Liegen nicht in einer Heimatplatz-Assembly, daher explizit statt
        // ueber den Assembly-Scan. Schema "ticker" wirkt nur auf Postgres - SQLite kennt keine
        // Schemas und legt die Tabellen ohne Praefix an.
        modelBuilder.ApplyConfiguration(new TimeTickerConfigurations<TimeTickerEntity>());
        modelBuilder.ApplyConfiguration(new CronTickerConfigurations<CronTickerEntity>());
        modelBuilder.ApplyConfiguration(new CronTickerOccurrenceConfigurations<CronTickerEntity>());

        ApplySqliteWorkaroundConverters(modelBuilder);
    }

    /// <summary>
    /// SQLite (nur lokale Entwicklung) kann DateTimeOffset und decimal nicht in
    /// ORDER BY / Aggregaten uebersetzen ("SQLite cannot order by expressions of type ...").
    /// Standard-EF-Workaround: als long (UTC-Ticks) bzw. double speichern - damit laufen
    /// Sortierung und Paging der Listen-Endpoints auf allen Providern in der Datenbank.
    /// Postgres/InMemory sind nicht betroffen (kein Konverter, keine Migration).
    /// ACHTUNG: aeltere lokale app.db-Dateien speichern die Werte noch als TEXT -
    /// einmalig loeschen, wird beim Start neu erstellt und geseedet.
    /// </summary>
    void ApplySqliteWorkaroundConverters(ModelBuilder modelBuilder)
    {
        if (!Database.IsSqlite())
            return;

        var dateTimeOffsetToTicks = new ValueConverter<DateTimeOffset, long>(
            v => v.UtcTicks,
            v => new DateTimeOffset(v, TimeSpan.Zero));
        var decimalToDouble = new ValueConverter<decimal, double>(
            v => (double)v,
            v => (decimal)v);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                var type = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
                if (type == typeof(DateTimeOffset))
                    property.SetValueConverter(dateTimeOffsetToTicks);
                else if (type == typeof(decimal))
                    property.SetValueConverter(decimalToDouble);
            }
        }
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                // Nur setzen wenn noch nicht explizit gesetzt (z.B. durch Seeder)
                if (entry.Entity.CreatedAt == default)
                    entry.Entity.CreatedAt = DateTimeOffset.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }
    }
}
