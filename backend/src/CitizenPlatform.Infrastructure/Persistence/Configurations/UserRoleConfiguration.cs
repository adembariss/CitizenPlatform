using CitizenPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CitizenPlatform.Infrastructure.Persistence.Configurations;

internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles", "public");
        builder.ConfigureAuditableEntity();

        builder.Property(entity => entity.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(entity => entity.RoleId)
            .HasColumnName("role_id")
            .IsRequired();

        builder.Property(entity => entity.MunicipalityId)
            .HasColumnName("municipality_id");

        builder.Property(entity => entity.AssignedAt)
            .HasColumnName("assigned_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(entity => entity.RevokedAt)
            .HasColumnName("revoked_at")
            .HasColumnType("timestamp with time zone");

        builder.Ignore(entity => entity.IsActive);

        builder.HasIndex(entity => new { entity.UserId, entity.RoleId, entity.MunicipalityId })
            .IsUnique()
            .HasFilter("revoked_at IS NULL AND is_deleted = false");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(entity => entity.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Municipality>()
            .WithMany()
            .HasForeignKey(entity => entity.MunicipalityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
