using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Features.PublicCategories;
using CitizenPlatform.Domain.Entities;
using Xunit;

namespace CitizenPlatform.UnitTests;

public sealed class PublicCategoryListQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenMunicipalityDoesNotExist_ReturnsNull()
    {
        var handler = new PublicCategoryListQueryHandler(
            new FakeMunicipalityRepository(null),
            new FakeComplaintCategoryRepository());

        var result = await handler.HandleAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAsync_WhenMunicipalityIsInactive_ReturnsNull()
    {
        var municipality = Municipality.Create("Demo Belediyesi", "DEMO");
        municipality.Deactivate();

        var handler = new PublicCategoryListQueryHandler(
            new FakeMunicipalityRepository(municipality),
            new FakeComplaintCategoryRepository());

        var result = await handler.HandleAsync(municipality.Id, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAsync_ReturnsOnlyActiveCategoriesOrderedByName()
    {
        var municipality = Municipality.Create("Demo Belediyesi", "DEMO");

        var active = ComplaintCategory.Create("Yol ve Kaldirim", "ROAD", municipality.Id);
        var globalActive = ComplaintCategory.Create("Genel", "GENERAL");
        var inactive = ComplaintCategory.Create("Eski Kategori", "OLD", municipality.Id);
        inactive.Deactivate();

        var categoryRepository = new FakeComplaintCategoryRepository(active, globalActive, inactive);
        var handler = new PublicCategoryListQueryHandler(
            new FakeMunicipalityRepository(municipality),
            categoryRepository);

        var result = await handler.HandleAsync(municipality.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(municipality.Id, categoryRepository.LastMunicipalityIdRequested);
        Assert.Equal(2, result!.Count);
        Assert.Equal(new[] { "Genel", "Yol ve Kaldirim" }, result.Select(category => category.Name).ToArray());
        Assert.DoesNotContain(result, category => category.Code == "OLD");
    }

    private sealed class FakeMunicipalityRepository : IMunicipalityRepository
    {
        private readonly Municipality? _municipality;

        public FakeMunicipalityRepository(Municipality? municipality)
        {
            _municipality = municipality;
        }

        public Task<Municipality?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(_municipality?.Id == id ? _municipality : null);
        }

        public Task<IReadOnlyList<string>> GetProvincesAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

        public Task<IReadOnlyList<DistrictRow>> GetDistrictsByProvinceAsync(string province, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<DistrictRow>>(Array.Empty<DistrictRow>());
    }

    private sealed class FakeComplaintCategoryRepository : IComplaintCategoryRepository
    {
        private readonly List<ComplaintCategory> _categories;

        public FakeComplaintCategoryRepository(params ComplaintCategory[] categories)
        {
            _categories = categories.ToList();
        }

        public Guid? LastMunicipalityIdRequested { get; private set; }

        public Task<ComplaintCategory?> GetActiveForMunicipalityAsync(Guid categoryId, Guid municipalityId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_categories.FirstOrDefault(category =>
                category.Id == categoryId
                && category.IsActive
                && (category.MunicipalityId is null || category.MunicipalityId == municipalityId)));
        }

        public Task<ComplaintCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(_categories.FirstOrDefault(category => category.Id == id));
        }

        public Task<IReadOnlyList<ComplaintCategory>> ListAsync(Guid? municipalityId, CancellationToken cancellationToken)
        {
            LastMunicipalityIdRequested = municipalityId;

            IReadOnlyList<ComplaintCategory> result = municipalityId is null
                ? _categories
                : _categories.Where(category => category.MunicipalityId is null || category.MunicipalityId == municipalityId).ToList();

            return Task.FromResult(result);
        }

        public Task<bool> CodeExistsAsync(Guid? municipalityId, string code, CancellationToken cancellationToken)
        {
            return Task.FromResult(_categories.Any(category => category.Code == code && category.MunicipalityId == municipalityId));
        }

        public Task AddAsync(ComplaintCategory category, CancellationToken cancellationToken)
        {
            _categories.Add(category);
            return Task.CompletedTask;
        }
    }
}
