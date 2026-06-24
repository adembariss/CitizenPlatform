using System.Linq.Expressions;
using CitizenPlatform.Domain.Common;
using CitizenPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CitizenPlatform.Infrastructure.Persistence;

public sealed class CitizenPlatformDbContext : DbContext
{
    public CitizenPlatformDbContext(DbContextOptions<CitizenPlatformDbContext> options)
        : base(options)
    {
    }

    public DbSet<Municipality> Municipalities => Set<Municipality>();

    public DbSet<MunicipalityBoundary> MunicipalityBoundaries => Set<MunicipalityBoundary>();

    public DbSet<MunicipalityDatabaseConnection> MunicipalityDatabaseConnections => Set<MunicipalityDatabaseConnection>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<Citizen> Citizens => Set<Citizen>();

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<ComplaintCategory> ComplaintCategories => Set<ComplaintCategory>();

    public DbSet<CategoryDepartmentRule> CategoryDepartmentRules => Set<CategoryDepartmentRule>();

    public DbSet<Complaint> Complaints => Set<Complaint>();

    public DbSet<ComplaintAttachment> ComplaintAttachments => Set<ComplaintAttachment>();

    public DbSet<ComplaintStatusHistory> ComplaintStatusHistories => Set<ComplaintStatusHistory>();

    public DbSet<ComplaintComment> ComplaintComments => Set<ComplaintComment>();

    public DbSet<ComplaintAssignment> ComplaintAssignments => Set<ComplaintAssignment>();

    public DbSet<IntegrationOutboxMessage> IntegrationOutbox => Set<IntegrationOutboxMessage>();

    public DbSet<IntegrationAttempt> IntegrationAttempts => Set<IntegrationAttempt>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditValues();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ApplyAuditValues();
        return base.SaveChanges();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");
        modelBuilder.HasPostgresExtension("postgis");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CitizenPlatformDbContext).Assembly);
        ApplySoftDeleteQueryFilters(modelBuilder);
    }

    private void ApplyAuditValues()
    {
        var utcNow = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Property(nameof(AuditableEntity.CreatedAt)).CurrentValue = utcNow;
                    entry.Property(nameof(AuditableEntity.UpdatedAt)).CurrentValue = null;
                    entry.Property(nameof(AuditableEntity.IsDeleted)).CurrentValue = false;
                    break;
                case EntityState.Modified:
                    entry.Property(nameof(AuditableEntity.UpdatedAt)).CurrentValue = utcNow;
                    break;
                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Property(nameof(AuditableEntity.IsDeleted)).CurrentValue = true;
                    entry.Property(nameof(AuditableEntity.DeletedAt)).CurrentValue = utcNow;
                    entry.Property(nameof(AuditableEntity.UpdatedAt)).CurrentValue = utcNow;
                    break;
            }
        }
    }

    private static void ApplySoftDeleteQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(entityType => typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType)))
        {
            var parameter = Expression.Parameter(entityType.ClrType, "entity");
            var isDeletedProperty = Expression.Property(parameter, nameof(AuditableEntity.IsDeleted));
            var compareExpression = Expression.Equal(isDeletedProperty, Expression.Constant(false));
            var lambda = Expression.Lambda(compareExpression, parameter);

            entityType.SetQueryFilter(lambda);
        }
    }
}
