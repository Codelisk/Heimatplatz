using Heimatplatz.Api.Features.Dashboards.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heimatplatz.Api.Features.Dashboards.Data.Configurations;

/// <summary>
/// EF Core Konfiguration fuer UserDashboardRevision Entity
/// </summary>
public class UserDashboardRevisionConfiguration : IEntityTypeConfiguration<UserDashboardRevision>
{
    public void Configure(EntityTypeBuilder<UserDashboardRevision> builder)
    {
        builder.ToTable("UserDashboardRevisions");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.UserPrompt)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(r => r.DefinitionJson)
            .HasColumnType("TEXT");

        builder.Property(r => r.RawOutputExcerpt)
            .HasColumnType("TEXT");

        // Revisionen haengen am Dashboard und verschwinden mit ihm (Loeschen/Konto-Loeschung)
        builder.HasOne<UserDashboard>()
            .WithMany()
            .HasForeignKey(r => r.DashboardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.DashboardId);
    }
}
