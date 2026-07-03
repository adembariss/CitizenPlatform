namespace CitizenPlatform.Application.DTOs;

public sealed record AddComplaintAttachmentsResponseDto(
    Guid ComplaintId,
    string TrackingCode,
    IReadOnlyCollection<ComplaintAttachmentDto> Attachments);
