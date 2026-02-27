using Heimatplatz.Api.Features.ForeclosureAuctions.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heimatplatz.Api.Features.ForeclosureAuctions.Data.Configurations;

public class ForeclosureAuctionChangeConfiguration : IEntityTypeConfiguration<ForeclosureAuctionChange>
{
    public void Configure(EntityTypeBuilder<ForeclosureAuctionChange> builder)
    {
        builder.ToTable("ForeclosureAuctionChanges");

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

        builder.HasIndex(c => c.ForeclosureAuctionId);
        builder.HasIndex(c => c.ChangeType);
        builder.HasIndex(c => c.CreatedAt);
    }
}
