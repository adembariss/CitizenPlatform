using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Features.Pharmacies;
using CitizenPlatform.Domain.Entities;
using Xunit;

namespace CitizenPlatform.UnitTests;

public sealed class PharmacyQueryHandlerTests
{
    [Fact]
    public async Task Nearby_ComputesDistanceAndClampsLimit()
    {
        var pharmacy = Pharmacy.Create("Kadıköy Eczanesi", "İstanbul", "Kadıköy", null, null, 40.99, 29.03, isOnDuty: true);
        var repository = new FakePharmacyRepository(pharmacy);
        var handler = new NearbyPharmacyQueryHandler(repository);

        var result = await handler.HandleAsync(40.983, 29.030, onDutyOnly: true, limit: 99999, CancellationToken.None);

        Assert.Equal(NearbyPharmacyQueryHandler.MaxLimit, repository.LastLimit);
        Assert.True(repository.LastOnDutyOnly);
        var item = Assert.Single(result);
        Assert.NotNull(item.DistanceKm);
        Assert.InRange(item.DistanceKm!.Value, 0, 5); // ~0.8 km
    }

    [Fact]
    public async Task List_UsesDefaultLimit_AndForwardsFilters()
    {
        var repository = new FakePharmacyRepository();
        var handler = new PharmacyListQueryHandler(repository);

        await handler.HandleAsync("İstanbul", "Beşiktaş", onDutyOnly: false, limit: null, CancellationToken.None);

        Assert.Equal(PharmacyListQueryHandler.DefaultLimit, repository.LastLimit);
        Assert.Equal("İstanbul", repository.LastProvince);
        Assert.Equal("Beşiktaş", repository.LastDistrict);
    }

    private sealed class FakePharmacyRepository : IPharmacyRepository
    {
        private readonly IReadOnlyList<Pharmacy> _items;

        public FakePharmacyRepository(params Pharmacy[] items) => _items = items;

        public int LastLimit { get; private set; }
        public bool LastOnDutyOnly { get; private set; }
        public string? LastProvince { get; private set; }
        public string? LastDistrict { get; private set; }

        public Task<IReadOnlyList<Pharmacy>> ListAsync(string? province, string? district, bool onDutyOnly, int limit, CancellationToken cancellationToken)
        {
            LastLimit = limit; LastOnDutyOnly = onDutyOnly; LastProvince = province; LastDistrict = district;
            return Task.FromResult(_items);
        }

        public Task<IReadOnlyList<Pharmacy>> NearbyAsync(double latitude, double longitude, bool onDutyOnly, int limit, CancellationToken cancellationToken)
        {
            LastLimit = limit; LastOnDutyOnly = onDutyOnly;
            return Task.FromResult(_items);
        }
    }
}
