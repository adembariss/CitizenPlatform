using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Domain.Entities;
using CitizenPlatform.Domain.Enums;
using CitizenPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CitizenPlatform.Infrastructure.Seeding;

/// <summary>
/// Development-only convenience seeder for demo admin accounts. Must only be invoked
/// when the host environment is Development (see Program.cs) - it never runs in production.
/// Relies on the Demo Belediyesi municipality already seeded via
/// database/main-db/003_seed_demo_municipality.sql; if that municipality is missing it
/// logs a warning and skips instead of failing API startup.
/// </summary>
public sealed class DevelopmentDataSeeder
{
    public const string DemoPassword = "Demo123!";

    private const string DemoMunicipalityCode = "DEMO";

    private readonly CitizenPlatformDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DevelopmentDataSeeder> _logger;

    public DevelopmentDataSeeder(
        CitizenPlatformDbContext dbContext,
        IPasswordHasher passwordHasher,
        ILogger<DevelopmentDataSeeder> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var demoMunicipality = await _dbContext.Municipalities
            .FirstOrDefaultAsync(municipality => municipality.Code == DemoMunicipalityCode, cancellationToken);

        if (demoMunicipality is null)
        {
            _logger.LogWarning(
                "Demo municipality (code={Code}) not found; skipping development user seeding. " +
                "Run database/main-db/003_seed_demo_municipality.sql first.",
                DemoMunicipalityCode);
            return;
        }

        var systemAdminRole = await GetOrCreateRoleAsync("SYSTEM_ADMIN", "SystemAdmin", cancellationToken);
        var municipalityAdminRole = await GetOrCreateRoleAsync("MUNICIPALITY_ADMIN", "MunicipalityAdmin", cancellationToken);
        var municipalityEmployeeRole = await GetOrCreateRoleAsync("MUNICIPALITY_EMPLOYEE", "MunicipalityEmployee", cancellationToken);

        await GetOrCreateUserAsync(
            "systemadmin@demo.local", "Sistem Yoneticisi", UserType.SystemAdmin, systemAdminRole.Id, null, cancellationToken);
        await GetOrCreateUserAsync(
            "admin@demo.local", "Demo Belediye Yoneticisi", UserType.MunicipalityAdmin, municipalityAdminRole.Id, demoMunicipality.Id, cancellationToken);
        await GetOrCreateUserAsync(
            "employee@demo.local", "Demo Belediye Calisani", UserType.MunicipalityEmployee, municipalityEmployeeRole.Id, demoMunicipality.Id, cancellationToken);

        // Her (demo dışı) belediye için ayrı bir yönetici ve çalışan hesabı.
        // E-posta şeması: admin@{kod}.bel.tr ve memur@{kod}.bel.tr (kod küçük harf).
        // Örn. Gelibolu için: admin@gelibolu.bel.tr / memur@gelibolu.bel.tr (parola: Demo123!).
        var otherMunicipalities = await _dbContext.Municipalities
            .Where(municipality => municipality.Code != DemoMunicipalityCode)
            .OrderBy(municipality => municipality.Name)
            .ToListAsync(cancellationToken);

        foreach (var municipality in otherMunicipalities)
        {
            var codeSlug = municipality.Code.ToLowerInvariant();

            await GetOrCreateUserAsync(
                $"admin@{codeSlug}.bel.tr",
                $"{municipality.Name} Yöneticisi",
                UserType.MunicipalityAdmin,
                municipalityAdminRole.Id,
                municipality.Id,
                cancellationToken);

            await GetOrCreateUserAsync(
                $"memur@{codeSlug}.bel.tr",
                $"{municipality.Name} Çalışanı",
                UserType.MunicipalityEmployee,
                municipalityEmployeeRole.Id,
                municipality.Id,
                cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Role> GetOrCreateRoleAsync(string key, string name, CancellationToken cancellationToken)
    {
        var role = await _dbContext.Roles.FirstOrDefaultAsync(candidate => candidate.Key == key, cancellationToken);
        if (role is not null)
        {
            return role;
        }

        role = Role.Create(name, key, isSystemRole: true);
        await _dbContext.Roles.AddAsync(role, cancellationToken);
        return role;
    }

    private async Task GetOrCreateUserAsync(
        string email,
        string displayName,
        UserType userType,
        Guid roleId,
        Guid? municipalityId,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _dbContext.Users.FirstOrDefaultAsync(candidate => candidate.Email == normalizedEmail, cancellationToken);

        if (user is null)
        {
            user = User.Create(email, displayName, userType);
            user.SetPasswordHash(_passwordHasher.Hash(DemoPassword));
            await _dbContext.Users.AddAsync(user, cancellationToken);
        }

        var hasActiveRole = await _dbContext.UserRoles.AnyAsync(
            userRole => userRole.UserId == user.Id && userRole.RoleId == roleId && userRole.RevokedAt == null,
            cancellationToken);

        if (!hasActiveRole)
        {
            var assignment = UserRole.Assign(user.Id, roleId, municipalityId);
            await _dbContext.UserRoles.AddAsync(assignment, cancellationToken);
        }
    }
}
