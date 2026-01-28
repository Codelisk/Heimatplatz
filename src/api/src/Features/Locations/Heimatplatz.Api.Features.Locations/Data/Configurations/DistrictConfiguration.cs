using Heimatplatz.Api.Features.Locations.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heimatplatz.Api.Features.Locations.Data.Configurations;

public class DistrictConfiguration : IEntityTypeConfiguration<District>
{
    public void Configure(EntityTypeBuilder<District> builder)
    {
        builder.ToTable("Districts");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Key)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(d => d.Code)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(d => d.Key)
            .IsUnique();

        builder.HasIndex(d => d.FederalProvinceId);

        builder.HasMany(d => d.Municipalities)
            .WithOne(m => m.District)
            .HasForeignKey(m => m.DistrictId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
