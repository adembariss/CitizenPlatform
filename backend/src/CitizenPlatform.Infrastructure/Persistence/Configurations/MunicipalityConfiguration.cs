using CitizenPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CitizenPlatform.Infrastructure.Persistence.Configurations;

internal sealed class MunicipalityConfiguration : IEntityTypeConfiguration<Municipality>
{
    public void Configure(EntityTypeBuilder<Municipality> builder)
    {
        builder.ToTable("municipalities", "public");
        builder.ConfigureAuditableEntity();

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

        builder.Property(entity => entity.Province)
            .HasColumnName("province")
            .HasMaxLength(100);

        builder.Property(entity => entity.CenterLatitude)
            .HasColumnName("center_latitude");

        builder.Property(entity => entity.CenterLongitude)
            .HasColumnName("center_longitude");

        builder.HasIndex(entity => entity.Code)
            .IsUnique()
            .HasFilter("is_deleted = false");

        builder.HasMany(entity => entity.Boundaries)
            .WithOne()
            .HasForeignKey(entity => entity.MunicipalityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(entity => entity.Boundaries)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(entity => entity.Departments)
            .WithOne()
            .HasForeignKey(entity => entity.MunicipalityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(entity => entity.Departments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
