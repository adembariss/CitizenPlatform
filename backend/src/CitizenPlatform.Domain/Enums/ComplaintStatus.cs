namespace CitizenPlatform.Domain.Enums;

public enum ComplaintStatus
{
    New = 1,
    UnderReview = 2,
    Assigned = 3,
    InProgress = 4,
    WaitingForCitizen = 5,
    Resolved = 6,
    Closed = 7,
    Rejected = 8,
    Duplicate = 9,
    OutOfScope = 10
}
