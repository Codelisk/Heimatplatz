using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heimatplatz.Api.Features.Properties.Data.Configurations;

public class PropertyChangeConfiguration : IEntityTypeConfiguration<PropertyChange>
{
    public void Configure(EntityTypeBuilder<PropertyChange> builder)
    {
        builder.ToTable("PropertyChanges");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.ChangeType)
            .IsRequired()
            .HasMaxLength(20);

        // Delta-Sync fragt "alle Aenderungen seit X" ab; Retention loescht nach CreatedAt
        builder.HasIndex(c => c.CreatedAt);
        builder.HasIndex(c => c.PropertyId);
    }
}
