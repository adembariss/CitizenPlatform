using CitizenPlatform.Domain.Enums;

namespace CitizenPlatform.Application.Features.AdminComplaints;

public sealed record UpdateComplaintStatusCommand(
    Guid ComplaintId,
    ComplaintStatus NewStatus,
    string? Note,
    bool IsVisibleToCitizen,
    Guid ChangedByUserId);
