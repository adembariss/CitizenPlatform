using CitizenPlatform.Domain.Common;
using CitizenPlatform.Domain.Enums;
using CitizenPlatform.Domain.Events;
using CitizenPlatform.Domain.ValueObjects;

namespace CitizenPlatform.Domain.Entities;

public sealed class Complaint : Entity
{
    private Complaint(
        Guid id,
        Guid municipalityId,
        string title,
        string description,
        GeoCoordinate location,
        SubmissionChannel channel)
        : base(id)
    {
        MunicipalityId = municipalityId;
        Title = title;
        Description = description;
        Location = location;
        Channel = channel;
        Status = ComplaintStatus.Submitted;
        Priority = ComplaintPriority.Normal;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid MunicipalityId { get; }

    public string Title { get; private set; }

    public string Description { get; private set; }

    public GeoCoordinate Location { get; private set; }

    public SubmissionChannel Channel { get; }

    public ComplaintStatus Status { get; private set; }

    public ComplaintPriority Priority { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public static Complaint Submit(
        Guid municipalityId,
        string title,
        string description,
        GeoCoordinate location,
        SubmissionChannel channel)
    {
        if (municipalityId == Guid.Empty)
        {
            throw new ArgumentException("Municipality id is required.", nameof(municipalityId));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description is required.", nameof(description));
        }

        var complaint = new Complaint(Guid.NewGuid(), municipalityId, title.Trim(), description.Trim(), location, channel);
        complaint.AddDomainEvent(new ComplaintSubmittedDomainEvent(complaint.Id, municipalityId, DateTimeOffset.UtcNow));

        return complaint;
    }

    public void MarkInReview()
    {
        Status = ComplaintStatus.InReview;
    }

    public void AssignPriority(ComplaintPriority priority)
    {
        Priority = priority;
    }
}
