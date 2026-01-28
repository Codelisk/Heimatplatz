using Heimatplatz.Api.Features.Locations.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heimatplatz.Api.Features.Locations.Data.Configurations;

public class FederalProvinceConfiguration : IEntityTypeConfiguration<FederalProvince>
{
    public void Configure(EntityTypeBuilder<FederalProvince> builder)
    {
        builder.ToTable("FederalProvinces");

        builder.HasKey(fp => fp.Id);

        builder.Property(fp => fp.Key)
            .IsRequired()
            .HasMaxLength(2);

        builder.Property(fp => fp.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(fp => fp.Key)
            .IsUnique();

        builder.HasMany(fp => fp.Districts)
            .WithOne(d => d.FederalProvince)
            .HasForeignKey(d => d.FederalProvinceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
