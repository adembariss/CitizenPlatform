using CitizenPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CitizenPlatform.Infrastructure.Persistence.Configurations;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles", "public");
        builder.ConfigureAuditableEntity();

        builder.Property(entity => entity.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(entity => entity.Key)
            .HasColumnName("key")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(entity => entity.IsSystemRole)
            .HasColumnName("is_system_role")
            .IsRequired();

        builder.HasIndex(entity => entity.Key)
            .IsUnique()
            .HasFilter("is_deleted = false");
    }
}
