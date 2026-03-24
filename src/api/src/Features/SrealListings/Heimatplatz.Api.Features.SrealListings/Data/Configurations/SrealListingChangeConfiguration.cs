using Heimatplatz.Api.Features.SrealListings.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heimatplatz.Api.Features.SrealListings.Data.Configurations;

public class SrealListingChangeConfiguration : IEntityTypeConfiguration<SrealListingChange>
{
    public void Configure(EntityTypeBuilder<SrealListingChange> builder)
    {
        builder.ToTable("SrealListingChanges");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.ChangeType)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(c => c.ChangedFields)
            .HasMaxLength(4000);

        builder.Property(c => c.OldContentHash)
            .HasMaxLength(64);

        builder.Property(c => c.NewContentHash)
            .HasMaxLength(64);

        builder.HasIndex(c => c.SrealListingId);
        builder.HasIndex(c => c.ChangeType);
        builder.HasIndex(c => c.CreatedAt);
    }
}
