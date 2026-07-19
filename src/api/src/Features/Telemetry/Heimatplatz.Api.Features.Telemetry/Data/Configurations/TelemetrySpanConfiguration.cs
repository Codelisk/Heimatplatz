using Heimatplatz.Api.Features.Telemetry.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heimatplatz.Api.Features.Telemetry.Data.Configurations;

public class TelemetrySpanConfiguration : IEntityTypeConfiguration<TelemetrySpan>
{
    public void Configure(EntityTypeBuilder<TelemetrySpan> builder)
    {
        builder.ToTable("TelemetrySpans");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.TraceId).IsRequired().HasMaxLength(32);
        builder.Property(s => s.SpanId).IsRequired().HasMaxLength(16);
        builder.Property(s => s.ParentSpanId).HasMaxLength(16);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(512);
        builder.Property(s => s.Kind).IsRequired().HasMaxLength(16);
        builder.Property(s => s.StatusCode).IsRequired().HasMaxLength(16);
        builder.Property(s => s.StatusDescription).HasMaxLength(2000);
        builder.Property(s => s.HttpMethod).HasMaxLength(16);
        builder.Property(s => s.HttpRoute).HasMaxLength(512);
        builder.Property(s => s.UserId).HasMaxLength(64);
        builder.Property(s => s.ClientApp).HasMaxLength(128);
        builder.Property(s => s.AttributesJson).HasColumnType("TEXT");

        // Zeitfenster-Abfragen und Retention nach StartTimeUtc; Waterfall laedt per TraceId
        builder.HasIndex(s => s.StartTimeUtc);
        builder.HasIndex(s => s.TraceId);
    }
}
