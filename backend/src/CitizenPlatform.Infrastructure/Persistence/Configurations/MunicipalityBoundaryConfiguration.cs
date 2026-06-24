using CitizenPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CitizenPlatform.Infrastructure.Persistence.Configurations;

internal sealed class MunicipalityBoundaryConfiguration : IEntityTypeConfiguration<MunicipalityBoundary>
{
    public void Configure(EntityTypeBuilder<MunicipalityBoundary> builder)
    {
        builder.ToTable("municipality_boundaries", "public");
        builder.ConfigureAuditableEntity();

        builder.Property(entity => entity.MunicipalityId)
            .HasColumnName("municipality_id")
            .IsRequired();

        builder.Property(entity => entity.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(entity => entity.BoundaryGeometry)
            .HasColumnName("boundary_geometry")
            .HasColumnType("geometry(MultiPolygon,4326)")
            .HasConversion(
                value => GeometryConversion.ToMultiPolygon(value),
                value => GeometryConversion.ToWkt(value))
            .IsRequired();

        builder.Property(entity => entity.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.HasIndex(entity => entity.MunicipalityId);

        builder.HasIndex(entity => entity.BoundaryGeometry)
            .HasMethod("gist")
            .HasDatabaseName("ix_municipality_boundaries_boundary_geometry_gist");
    }
}
