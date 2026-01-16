using Heimatplatz.Api.Features.Auth.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heimatplatz.Api.Features.Auth.Data.Configurations;

/// <summary>
/// EF Core Konfiguration fuer UserRole Entity
/// </summary>
public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.UserId)
            .IsRequired();

        builder.Property(r => r.RoleType)
            .IsRequired();

        // Beziehung zu User
        builder.HasOne(r => r.User)
            .WithMany(u => u.Roles)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Eindeutiger Index: Ein User kann jede Rolle nur einmal haben
        builder.HasIndex(r => new { r.UserId, r.RoleType })
            .IsUnique();
    }
}
