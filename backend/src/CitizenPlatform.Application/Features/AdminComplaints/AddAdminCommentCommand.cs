namespace CitizenPlatform.Application.Features.AdminComplaints;

public sealed record AddAdminCommentCommand(
    Guid ComplaintId,
    string CommentText,
    bool IsInternal,
    Guid AuthorUserId);
