using CitizenPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CitizenPlatform.Infrastructure.Persistence.Configurations;

internal sealed class InstitutionConfiguration : IEntityTypeConfiguration<Institution>
{
    public void Configure(EntityTypeBuilder<Institution> builder)
    {
        builder.ToTable("institutions", "public");
        builder.ConfigureAuditableEntity();

        builder.Property(entity => entity.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(entity => entity.Type).HasColumnName("type").HasConversion<int>().IsRequired();
        builder.Property(entity => entity.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(entity => entity.Province).HasColumnName("province").HasMaxLength(100);
        builder.Property(entity => entity.CenterLatitude).HasColumnName("center_latitude");
        builder.Property(entity => entity.CenterLongitude).HasColumnName("center_longitude");

        builder.HasIndex(entity => entity.Code)
            .IsUnique()
            .HasFilter("is_deleted = false");

        builder.HasIndex(entity => entity.Type);

        builder.HasMany(entity => entity.ServiceAreas)
            .WithOne()
            .HasForeignKey(entity => entity.InstitutionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(entity => entity.ServiceAreas)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class InstitutionServiceAreaConfiguration : IEntityTypeConfiguration<InstitutionServiceArea>
{
    public void Configure(EntityTypeBuilder<InstitutionServiceArea> builder)
    {
        builder.ToTable("institution_service_areas", "public");
        builder.ConfigureAuditableEntity();

        builder.Property(entity => entity.InstitutionId).HasColumnName("institution_id").IsRequired();
        builder.Property(entity => entity.Province).HasColumnName("province").HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.District).HasColumnName("district").HasMaxLength(100);

        builder.HasIndex(entity => new { entity.Province, entity.District });
        builder.HasIndex(entity => entity.InstitutionId);
    }
}
