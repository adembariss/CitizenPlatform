using CitizenPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CitizenPlatform.Infrastructure.Persistence.Configurations;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs", "public");
        builder.ConfigureAuditableEntity();

        builder.Property(entity => entity.EntityName)
            .HasColumnName("entity_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(entity => entity.EntityId)
            .HasColumnName("entity_id")
            .IsRequired();

        builder.Property(entity => entity.Action)
            .HasColumnName("action")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(entity => entity.Changes)
            .HasColumnName("changes")
            .HasColumnType("jsonb");

        builder.Property(entity => entity.UserId)
            .HasColumnName("user_id");

        builder.Property(entity => entity.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(100);

        builder.HasIndex(entity => new { entity.EntityName, entity.EntityId });
        builder.HasIndex(entity => entity.UserId)
            .HasFilter("user_id IS NOT NULL");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
