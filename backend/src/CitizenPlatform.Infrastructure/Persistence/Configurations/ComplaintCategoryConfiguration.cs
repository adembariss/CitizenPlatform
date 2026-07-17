using CitizenPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CitizenPlatform.Infrastructure.Persistence.Configurations;

internal sealed class ComplaintCategoryConfiguration : IEntityTypeConfiguration<ComplaintCategory>
{
    public void Configure(EntityTypeBuilder<ComplaintCategory> builder)
    {
        builder.ToTable("complaint_categories", "public");
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

        builder.HasIndex(entity => new { entity.MunicipalityId, entity.Code })
            .IsUnique()
            .HasFilter("municipality_id IS NOT NULL AND is_deleted = false");

        // Kuruma özel kategoriler (municipality_id NULL, institution_id dolu) — kurum içinde kod tekil.
        builder.HasIndex(entity => new { entity.InstitutionId, entity.Code })
            .IsUnique()
            .HasFilter("institution_id IS NOT NULL AND is_deleted = false");

        // Gerçek global kategoriler (her ikisi de NULL) — kod tekil.
        builder.HasIndex(entity => entity.Code)
            .IsUnique()
            .HasFilter("municipality_id IS NULL AND institution_id IS NULL AND is_deleted = false");

        builder.HasOne<Municipality>()
            .WithMany()
            .HasForeignKey(entity => entity.MunicipalityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
