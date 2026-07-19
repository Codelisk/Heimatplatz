using Heimatplatz.Api.Features.Telemetry.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heimatplatz.Api.Features.Telemetry.Data.Configurations;

public class TelemetryErrorGroupConfiguration : IEntityTypeConfiguration<TelemetryErrorGroup>
{
    public void Configure(EntityTypeBuilder<TelemetryErrorGroup> builder)
    {
        builder.ToTable("TelemetryErrorGroups");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.FingerprintHash).IsRequired().HasMaxLength(64);
        builder.Property(g => g.ExceptionType).IsRequired().HasMaxLength(512);
        builder.Property(g => g.Title).IsRequired().HasMaxLength(512);
        builder.Property(g => g.SampleMessage).IsRequired().HasColumnType("TEXT");
        builder.Property(g => g.SampleStackTrace).HasColumnType("TEXT");
        builder.Property(g => g.LastTraceId).HasMaxLength(32);

        // Lesbar statt int, robust gegen Enum-Umsortierung
        builder.Property(g => g.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(12);

        // Upsert im TelemetryWriter sucht per Hash; Listen filtern nach Status + LastSeen
        builder.HasIndex(g => g.FingerprintHash).IsUnique();
        builder.HasIndex(g => new { g.Status, g.LastSeenUtc });
    }
}
