using CitizenPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CitizenPlatform.Infrastructure.Persistence.Configurations;

internal sealed class PharmacyConfiguration : IEntityTypeConfiguration<Pharmacy>
{
    public void Configure(EntityTypeBuilder<Pharmacy> builder)
    {
        builder.ToTable("pharmacies", "public");
        builder.ConfigureAuditableEntity();

        builder.Property(entity => entity.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.Province).HasColumnName("province").HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.District).HasColumnName("district").HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.AddressText).HasColumnName("address_text").HasMaxLength(1000);
        builder.Property(entity => entity.PhoneNumber).HasColumnName("phone_number").HasMaxLength(40);
        builder.Property(entity => entity.Latitude).HasColumnName("latitude").IsRequired();
        builder.Property(entity => entity.Longitude).HasColumnName("longitude").IsRequired();
        builder.Property(entity => entity.IsOnDuty).HasColumnName("is_on_duty").IsRequired();

        builder.HasIndex(entity => new { entity.Province, entity.District });
        builder.HasIndex(entity => entity.IsOnDuty);
    }
}
