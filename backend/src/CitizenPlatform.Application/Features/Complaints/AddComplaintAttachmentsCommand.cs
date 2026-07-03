using CitizenPlatform.Application.DTOs;

namespace CitizenPlatform.Application.Features.Complaints;

public sealed record AddComplaintAttachmentsCommand(
    string TrackingCode,
    IReadOnlyCollection<ComplaintAttachmentUpload>? Attachments);
