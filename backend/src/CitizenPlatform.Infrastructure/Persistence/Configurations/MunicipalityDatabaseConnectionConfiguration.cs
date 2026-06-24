using CitizenPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CitizenPlatform.Infrastructure.Persistence.Configurations;

internal sealed class MunicipalityDatabaseConnectionConfiguration : IEntityTypeConfiguration<MunicipalityDatabaseConnection>
{
    public void Configure(EntityTypeBuilder<MunicipalityDatabaseConnection> builder)
    {
        builder.ToTable("municipality_database_connections", "public");
        builder.ConfigureAuditableEntity();

        builder.Property(entity => entity.MunicipalityId)
            .HasColumnName("municipality_id")
            .IsRequired();

        builder.Property(entity => entity.Provider)
            .HasColumnName("provider")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(entity => entity.ConnectionName)
            .HasColumnName("connection_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(entity => entity.EncryptedConnectionString)
            .HasColumnName("encrypted_connection_string")
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(entity => entity.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.HasIndex(entity => new { entity.MunicipalityId, entity.ConnectionName })
            .IsUnique()
            .HasFilter("is_deleted = false");

        builder.HasOne<Municipality>()
            .WithMany()
            .HasForeignKey(entity => entity.MunicipalityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
