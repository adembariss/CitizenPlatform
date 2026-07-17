using CitizenPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CitizenPlatform.Infrastructure.Persistence.Configurations;

internal sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("departments", "public");
        builder.ConfigureAuditableEntity();

        builder.Property(entity => entity.MunicipalityId)
            .HasColumnName("municipality_id");

        builder.Property(entity => entity.InstitutionId)
            .HasColumnName("institution_id");

        builder.Property(entity => entity.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(entity => entity.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(entity => entity.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        // Belediye birimleri: kod belediye içinde tekil.
        builder.HasIndex(entity => new { entity.MunicipalityId, entity.Code })
            .IsUnique()
            .HasFilter("municipality_id IS NOT NULL AND is_deleted = false");

        // Kurum birimleri: kod kurum içinde tekil (belediye birimlerinden ayrı yapı).
        builder.HasIndex(entity => new { entity.InstitutionId, entity.Code })
            .IsUnique()
            .HasFilter("institution_id IS NOT NULL AND is_deleted = false");
    }
}
