using Heimatplatz.Api.Features.Telemetry.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heimatplatz.Api.Features.Telemetry.Data.Configurations;

public class TelemetryLogConfiguration : IEntityTypeConfiguration<TelemetryLog>
{
    public void Configure(EntityTypeBuilder<TelemetryLog> builder)
    {
        builder.ToTable("TelemetryLogs");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.TraceId).HasMaxLength(32);
        builder.Property(l => l.SpanId).HasMaxLength(16);
        builder.Property(l => l.Level).IsRequired().HasMaxLength(12);
        builder.Property(l => l.Category).IsRequired().HasMaxLength(256);
        builder.Property(l => l.MessageTemplate).HasColumnType("TEXT");
        builder.Property(l => l.Message).IsRequired().HasColumnType("TEXT");
        builder.Property(l => l.ExceptionType).HasMaxLength(512);
        builder.Property(l => l.ExceptionMessage).HasColumnType("TEXT");
        builder.Property(l => l.ExceptionStackTrace).HasColumnType("TEXT");
        builder.Property(l => l.UserId).HasMaxLength(64);
        builder.Property(l => l.ClientApp).HasMaxLength(128);
        builder.Property(l => l.AttributesJson).HasColumnType("TEXT");

        // Lesbar statt int, robust gegen Enum-Umsortierung
        builder.Property(l => l.Source)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(8);

        // Fehlergruppen bleiben dauerhaft, Logs werden von der Retention getrimmt -
        // bewusst keine Navigation und kein Kaskadenverhalten
        builder.HasIndex(l => l.TimestampUtc);
        builder.HasIndex(l => l.TraceId);
        builder.HasIndex(l => new { l.ErrorGroupId, l.TimestampUtc });
        builder.HasIndex(l => new { l.Level, l.TimestampUtc });
    }
}
