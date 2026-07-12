using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Features.Complaints;
using CitizenPlatform.Domain.Entities;
using CitizenPlatform.Domain.Enums;
using CitizenPlatform.Domain.ValueObjects;
using Xunit;

namespace CitizenPlatform.UnitTests;

public sealed class TrackComplaintQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenTrackingCodeIsUnknown_ReturnsNull()
    {
        var handler = new TrackComplaintQueryHandler(new FakePublicComplaintTrackingRepository(null));

        var result = await handler.HandleAsync("BLD-2026-YOKBOYLE", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAsync_WhenTrackingCodeIsBlank_ReturnsNullWithoutQuerying()
    {
        var repository = new FakePublicComplaintTrackingRepository(null);
        var handler = new TrackComplaintQueryHandler(repository);

        var result = await handler.HandleAsync("   ", CancellationToken.None);

        Assert.Null(result);
        Assert.False(repository.WasQueried);
    }

    [Fact]
    public async Task HandleAsync_TrimsTrackingCodeBeforeQuerying()
    {
        var complaint = CreateComplaint();
        var repository = new FakePublicComplaintTrackingRepository(
            new PublicComplaintTrackingRow(complaint, "Demo Belediyesi", "Yol ve Kaldirim", null));
        var handler = new TrackComplaintQueryHandler(repository);

        var result = await handler.HandleAsync("  BLD-2026-A8F21C  ", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("BLD-2026-A8F21C", repository.LastTrackingCodeRequested);
    }

    [Fact]
    public async Task HandleAsync_NeverExposesHiddenHistoryInternalCommentsOrUserIds()
    {
        var complaint = CreateComplaint();
        complaint.RecordInitialStatus();

        var adminUserId = Guid.NewGuid();
        complaint.ChangeStatus(ComplaintStatus.UnderReview, adminUserId, "GIZLI ic degerlendirme notu", isVisibleToCitizen: false);
        complaint.ChangeStatus(ComplaintStatus.InProgress, adminUserId, "Ekip yola cikti.", isVisibleToCitizen: true);
        complaint.AddComment(adminUserId, "GIZLI ic yorum", isInternal: true);
        complaint.AddComment(adminUserId, "Sikayetiniz ekibimize iletildi.", isInternal: false);

        var repository = new FakePublicComplaintTrackingRepository(
            new PublicComplaintTrackingRow(complaint, "Demo Belediyesi", "Yol ve Kaldirim", "Fen Isleri"));
        var handler = new TrackComplaintQueryHandler(repository);

        var result = await handler.HandleAsync(complaint.TrackingCode, CancellationToken.None);

        Assert.NotNull(result);

        // Only the citizen-visible history entries are returned.
        Assert.Equal(2, result!.StatusHistory.Count);
        Assert.DoesNotContain(result.StatusHistory, entry => entry.Note is not null && entry.Note.Contains("GIZLI"));
        Assert.Contains(result.StatusHistory, entry => entry.NewStatus == ComplaintStatus.InProgress && entry.Note == "Ekip yola cikti.");

        // Internal comments never leak; public replies carry no author user id.
        var response = Assert.Single(result.Responses);
        Assert.Equal("Sikayetiniz ekibimize iletildi.", response.Body);

        Assert.Equal(complaint.TrackingCode, result.TrackingCode);
        Assert.Equal(ComplaintStatus.InProgress, result.Status);
        Assert.Equal("Fen Isleri", result.DepartmentName);
    }

    private static Complaint CreateComplaint()
    {
        return Complaint.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "BLD-2026-A8F21C",
            "Kaldirim hasari",
            "Mahalle girisindeki kaldirim hasarli.",
            new GeoCoordinate(41.05, 29.00),
            ComplaintSource.CitizenWeb);
    }

    private sealed class FakePublicComplaintTrackingRepository : IPublicComplaintTrackingRepository
    {
        private readonly PublicComplaintTrackingRow? _row;

        public FakePublicComplaintTrackingRepository(PublicComplaintTrackingRow? row)
        {
            _row = row;
        }

        public bool WasQueried { get; private set; }

        public string? LastTrackingCodeRequested { get; private set; }

        public Task<PublicComplaintTrackingRow?> GetByTrackingCodeAsync(string trackingCode, CancellationToken cancellationToken)
        {
            WasQueried = true;
            LastTrackingCodeRequested = trackingCode;

            if (_row is null || _row.Complaint.TrackingCode != trackingCode)
            {
                return Task.FromResult<PublicComplaintTrackingRow?>(null);
            }

            return Task.FromResult<PublicComplaintTrackingRow?>(_row);
        }
    }
}
