using Heimatplatz.Api.Features.Locations.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heimatplatz.Api.Features.Locations.Data.Configurations;

public class MunicipalityConfiguration : IEntityTypeConfiguration<Municipality>
{
    public void Configure(EntityTypeBuilder<Municipality> builder)
    {
        builder.ToTable("Municipalities");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Key)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(m => m.Code)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(m => m.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.PostalCode)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(m => m.Status)
            .HasMaxLength(50);

        builder.HasIndex(m => m.Key)
            .IsUnique();

        builder.HasIndex(m => m.DistrictId);

        builder.HasIndex(m => m.Name);
    }
}
