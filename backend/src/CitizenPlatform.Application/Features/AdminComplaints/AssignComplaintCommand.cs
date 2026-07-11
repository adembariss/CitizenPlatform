namespace CitizenPlatform.Application.Features.AdminComplaints;

public sealed record AssignComplaintCommand(
    Guid ComplaintId,
    Guid DepartmentId,
    Guid? AssignedUserId,
    string? Note,
    Guid AssignedByUserId);
