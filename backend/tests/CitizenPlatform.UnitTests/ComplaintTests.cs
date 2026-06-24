using CitizenPlatform.Domain.Entities;
using CitizenPlatform.Domain.Enums;
using CitizenPlatform.Domain.ValueObjects;
using Xunit;

namespace CitizenPlatform.UnitTests;

public sealed class ComplaintTests
{
    [Fact]
    public void Create_WhenValid_SetsStatusToNew()
    {
        var complaint = CreateComplaint();

        Assert.Equal(ComplaintStatus.New, complaint.Status);
        Assert.Equal("POINT (28.9784 41.0082)", complaint.LocationGeometry);
    }

    [Fact]
    public void ChangeStatus_WhenStatusChanges_AddsHistory()
    {
        var complaint = CreateComplaint();
        var changedByUserId = Guid.NewGuid();

        var history = complaint.ChangeStatus(ComplaintStatus.UnderReview, changedByUserId, "Initial review started.");

        Assert.Equal(ComplaintStatus.UnderReview, complaint.Status);
        Assert.Single(complaint.StatusHistories);
        Assert.Equal(ComplaintStatus.New, history.PreviousStatus);
        Assert.Equal(ComplaintStatus.UnderReview, history.NewStatus);
        Assert.Equal(changedByUserId, history.ChangedByUserId);
    }

    [Fact]
    public void AssignToDepartment_WhenValid_AssignsDepartmentAndCreatesAssignment()
    {
        var complaint = CreateComplaint();
        var departmentId = Guid.NewGuid();
        var assignedByUserId = Guid.NewGuid();
        var assignedUserId = Guid.NewGuid();

        var assignment = complaint.AssignToDepartment(departmentId, assignedByUserId, assignedUserId, "Route to public works.");

        Assert.Equal(ComplaintStatus.Assigned, complaint.Status);
        Assert.Equal(departmentId, complaint.CurrentDepartmentId);
        Assert.Equal(assignedUserId, complaint.AssignedUserId);
        Assert.Single(complaint.Assignments);
        Assert.Equal(departmentId, assignment.DepartmentId);
        Assert.Equal(assignedByUserId, assignment.AssignedByUserId);
    }

    private static Complaint CreateComplaint()
    {
        return Complaint.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "CP-2026-000001",
            "Yol bakım talebi",
            "Mahalle girişindeki yol hasarlı.",
            new GeoCoordinate(41.0082, 28.9784),
            ComplaintSource.CitizenWeb,
            Guid.NewGuid());
    }
}
