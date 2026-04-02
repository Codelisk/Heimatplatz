using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heimatplatz.Api.Features.Properties.Data.Configurations;

/// <summary>
/// EF Core configuration for SellerSource entity
/// </summary>
public class SellerSourceConfiguration : IEntityTypeConfiguration<SellerSource>
{
    public void Configure(EntityTypeBuilder<SellerSource> builder)
    {
        builder.ToTable("SellerSources");

        builder.HasKey(ss => ss.Id);

        builder.Property(ss => ss.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(ss => ss.IsDefault)
            .IsRequired()
            .HasDefaultValue(true);

        // Unique index on Name
        builder.HasIndex(ss => ss.Name)
            .IsUnique();
    }
}
