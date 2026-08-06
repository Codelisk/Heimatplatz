using Heimatplatz.Api.Features.Dashboards.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heimatplatz.Api.Features.Dashboards.Data.Configurations;

/// <summary>
/// EF Core Konfiguration fuer UserDashboard Entity
/// </summary>
public class UserDashboardConfiguration : IEntityTypeConfiguration<UserDashboard>
{
    public void Configure(EntityTypeBuilder<UserDashboard> builder)
    {
        builder.ToTable("UserDashboards");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.UserId)
            .IsRequired();

        builder.Property(d => d.Title)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(d => d.DefinitionJson)
            .HasColumnType("TEXT");

        builder.Property(d => d.GenerationError)
            .HasMaxLength(2000);

        builder.HasIndex(d => d.UserId);
    }
}
