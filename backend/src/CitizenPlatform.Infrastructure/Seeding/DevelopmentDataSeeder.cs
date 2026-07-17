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
        // Toplu içe aktarılan ilçeler (TR_*) için personel hesabı üretilmez; bunlar
        // ~1820 kullanıcı + parola hash'lemesiyle açılışı dakikalarca yavaşlatırdı.
        // Bu belediyelerin şikayetleri Sistem Yöneticisi (systemadmin) tarafından görülür.
        var otherMunicipalities = await _dbContext.Municipalities
            .Where(municipality => municipality.Code != DemoMunicipalityCode && !municipality.Code.StartsWith("TR_"))
            .OrderBy(municipality => municipality.Name)
            .ToListAsync(cancellationToken);

        foreach (var municipality in otherMunicipalities)
        {
            // Underscores are invalid in e-mail domains (browser type="email" rejects them),
            // so map them to hyphens: IST_KADIKOY -> admin@ist-kadikoy.bel.tr.
            var codeSlug = municipality.Code.ToLowerInvariant().Replace('_', '-');

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

        // Türkiye geneli dağıtım kurumları (elektrik/su/doğalgaz) + hizmet bölgeleri + hesaplar.
        await SeedInstitutionsAsync(municipalityAdminRole.Id, municipalityEmployeeRole.Id, cancellationToken);

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
        CancellationToken cancellationToken,
        Guid? institutionId = null)
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
            var assignment = UserRole.Assign(user.Id, roleId, municipalityId, institutionId);
            await _dbContext.UserRoles.AddAsync(assignment, cancellationToken);
        }
    }

    // Türkiye geneli dağıtım kurumlarını (elektrik/su/doğalgaz) hizmet bölgeleri, örnek
    // kategoriler ve demo admin/memur hesaplarıyla seed'ler. Idempotent (koda göre get-or-create).
    private async Task SeedInstitutionsAsync(Guid adminRoleId, Guid employeeRoleId, CancellationToken cancellationToken)
    {
        var existingInstitutions = await _dbContext.Institutions.ToListAsync(cancellationToken);
        var byCode = existingInstitutions.ToDictionary(institution => institution.Code, StringComparer.Ordinal);
        var institutionIdsWithDepartments = await _dbContext.Departments
            .Where(department => department.InstitutionId != null)
            .Select(department => department.InstitutionId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);
        var hasDepartments = institutionIdsWithDepartments.ToHashSet();

        foreach (var seed in InstitutionSeedData.Institutions)
        {
            var code = seed.Code.ToUpperInvariant();
            if (!byCode.TryGetValue(code, out var institution))
            {
                institution = Institution.Create(seed.Name, code, seed.Type, seed.CenterProvince);
                foreach (var province in seed.Provinces)
                {
                    institution.AddServiceArea(province);
                }

                await _dbContext.Institutions.AddAsync(institution, cancellationToken);

                foreach (var (name, categoryCode) in InstitutionSeedData.Categories[seed.Type])
                {
                    var category = ComplaintCategory.Create(name, $"{categoryCode}_{code}", municipalityId: null, institutionId: institution.Id);
                    await _dbContext.ComplaintCategories.AddAsync(category, cancellationToken);
                }

                var codeSlug = code.ToLowerInvariant().Replace('_', '-');
                await GetOrCreateUserAsync(
                    $"admin@{codeSlug}.kurum.tr", $"{seed.Name} Yöneticisi",
                    UserType.MunicipalityAdmin, adminRoleId, null, cancellationToken, institution.Id);
                await GetOrCreateUserAsync(
                    $"memur@{codeSlug}.kurum.tr", $"{seed.Name} Çalışanı",
                    UserType.MunicipalityEmployee, employeeRoleId, null, cancellationToken, institution.Id);
            }

            // Kuruma özel birimler (belediye birimlerinden ayrı yapı). Daha önce oluşturulmuş
            // kurumlara da eklenir; bu yüzden kurum oluşturmadan ayrı kontrol edilir.
            if (!hasDepartments.Contains(institution.Id))
            {
                foreach (var (name, departmentCode) in InstitutionSeedData.Departments[seed.Type])
                {
                    var department = Department.CreateForInstitution(institution.Id, name, departmentCode);
                    await _dbContext.Departments.AddAsync(department, cancellationToken);
                }
            }
        }
    }
}
