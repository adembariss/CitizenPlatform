using CitizenPlatform.Domain.Common;
using CitizenPlatform.Domain.Enums;
using CitizenPlatform.Domain.Events;
using CitizenPlatform.Domain.ValueObjects;

namespace CitizenPlatform.Domain.Entities;

public sealed class Complaint : AuditableEntity
{
    private readonly List<ComplaintAttachment> _attachments = [];
    private readonly List<ComplaintStatusHistory> _statusHistories = [];
    private readonly List<ComplaintComment> _comments = [];
    private readonly List<ComplaintAssignment> _assignments = [];

    private Complaint()
    {
    }

    private Complaint(
        Guid id,
        Guid municipalityId,
        Guid categoryId,
        string trackingCode,
        string title,
        string description,
        string? addressText,
        GeoCoordinate location,
        ComplaintSource source,
        Guid? citizenId,
        GeoCoordinate? photoExifLocation,
        ComplaintPriority priority,
        DateTimeOffset? createdAt)
        : base(id)
    {
        var occurredOn = createdAt ?? DateTimeOffset.UtcNow;

        MunicipalityId = Guard.AgainstEmpty(municipalityId, nameof(municipalityId));
        CategoryId = Guard.AgainstEmpty(categoryId, nameof(categoryId));
        TrackingCode = Guard.AgainstEmpty(trackingCode, nameof(trackingCode), 64);
        Title = Guard.AgainstEmpty(title, nameof(title), 200);
        Description = Guard.AgainstEmpty(description, nameof(description), 4000);
        AddressText = string.IsNullOrWhiteSpace(addressText) ? null : addressText.Trim();
        Location = location ?? throw new ArgumentNullException(nameof(location));
        LocationGeometry = location.ToWktPoint();
        PhotoExifLocation = photoExifLocation;
        PhotoExifGeometry = photoExifLocation?.ToWktPoint();
        Source = source;
        CitizenId = citizenId == Guid.Empty ? null : citizenId;
        Status = ComplaintStatus.New;
        Priority = priority;
        CreatedAt = occurredOn;

        AddDomainEvent(new ComplaintSubmittedDomainEvent(Id, MunicipalityId, TrackingCode, occurredOn));
    }

    public Guid MunicipalityId { get; private set; }

    public Guid CategoryId { get; private set; }

    public Guid? CitizenId { get; private set; }

    public string TrackingCode { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public string? AddressText { get; private set; }

    public GeoCoordinate Location { get; private set; } = null!;

    public string LocationGeometry { get; private set; } = string.Empty;

    public GeoCoordinate? PhotoExifLocation { get; private set; }

    public string? PhotoExifGeometry { get; private set; }

    public ComplaintStatus Status { get; private set; }

    public ComplaintPriority Priority { get; private set; }

    public ComplaintSource Source { get; private set; }

    public Guid? CurrentDepartmentId { get; private set; }

    public Guid? AssignedUserId { get; private set; }

    public string? ExternalMunicipalityComplaintId { get; private set; }

    public string? ExternalMunicipalityStatus { get; private set; }

    public DateTimeOffset? LastSyncAttemptAt { get; private set; }

    public DateTimeOffset? SyncedAt { get; private set; }

    public DateTimeOffset? ClosedAt { get; private set; }

    public IReadOnlyCollection<ComplaintAttachment> Attachments => _attachments.AsReadOnly();

    public IReadOnlyCollection<ComplaintStatusHistory> StatusHistories => _statusHistories.AsReadOnly();

    public IReadOnlyCollection<ComplaintComment> Comments => _comments.AsReadOnly();

    public IReadOnlyCollection<ComplaintAssignment> Assignments => _assignments.AsReadOnly();

    public static Complaint Create(
        Guid municipalityId,
        Guid categoryId,
        string trackingCode,
        string title,
        string description,
        GeoCoordinate location,
        ComplaintSource source,
        Guid? citizenId = null,
        GeoCoordinate? photoExifLocation = null,
        ComplaintPriority priority = ComplaintPriority.Normal,
        string? addressText = null,
        DateTimeOffset? createdAt = null)
    {
        return new Complaint(
            Guid.NewGuid(),
            municipalityId,
            categoryId,
            trackingCode,
            title,
            description,
            addressText,
            location,
            source,
            citizenId,
            photoExifLocation,
            priority,
            createdAt);
    }

    public ComplaintStatusHistory RecordInitialStatus(string? note = null)
    {
        if (_statusHistories.Any(history => history.PreviousStatus is null && history.NewStatus == ComplaintStatus.New))
        {
            throw new InvalidOperationException("Initial status history has already been recorded.");
        }

        var history = ComplaintStatusHistory.CreateInitial(Id, note);
        _statusHistories.Add(history);
        Touch();

        return history;
    }

    public void RouteToDepartment(Guid departmentId)
    {
        CurrentDepartmentId = Guard.AgainstEmpty(departmentId, nameof(departmentId));
        Touch();
    }

    public ComplaintStatusHistory ChangeStatus(
        ComplaintStatus newStatus,
        Guid changedByUserId,
        string? note = null,
        bool isVisibleToCitizen = true)
    {
        Guard.AgainstEmpty(changedByUserId, nameof(changedByUserId));

        if (Status == newStatus)
        {
            throw new InvalidOperationException($"Complaint is already in {newStatus} status.");
        }

        var previousStatus = Status;
        Status = newStatus;

        if (newStatus is ComplaintStatus.Resolved or ComplaintStatus.Closed)
        {
            ClosedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            ClosedAt = null;
        }

        var history = ComplaintStatusHistory.Create(Id, previousStatus, newStatus, changedByUserId, note, isVisibleToCitizen);
        _statusHistories.Add(history);

        Touch();
        AddDomainEvent(new ComplaintStatusChangedDomainEvent(Id, previousStatus, newStatus, changedByUserId, DateTimeOffset.UtcNow));

        return history;
    }

    public ComplaintAssignment AssignToDepartment(
        Guid departmentId,
        Guid assignedByUserId,
        Guid? assignedUserId = null,
        string? note = null)
    {
        Guard.AgainstEmpty(departmentId, nameof(departmentId));
        Guard.AgainstEmpty(assignedByUserId, nameof(assignedByUserId));

        CurrentDepartmentId = departmentId;
        AssignedUserId = assignedUserId == Guid.Empty ? null : assignedUserId;

        var assignment = ComplaintAssignment.Create(Id, departmentId, assignedByUserId, AssignedUserId, note);
        _assignments.Add(assignment);

        if (Status != ComplaintStatus.Assigned)
        {
            ChangeStatus(ComplaintStatus.Assigned, assignedByUserId, "Complaint assigned to department.");
        }
        else
        {
            Touch();
        }

        return assignment;
    }

    public ComplaintComment AddComment(Guid authorUserId, string body, bool isInternal = false)
    {
        var comment = ComplaintComment.Create(Id, authorUserId, body, isInternal);
        _comments.Add(comment);
        Touch();

        return comment;
    }

    public ComplaintAttachment AddAttachment(
        string fileName,
        string originalFileName,
        string contentType,
        long sizeInBytes,
        string sha256Hash,
        StorageProvider storageProvider,
        string objectKey,
        Guid? uploadedByUserId = null,
        GeoCoordinate? photoExifLocation = null,
        DateTimeOffset? photoTakenAt = null)
    {
        var attachment = ComplaintAttachment.Create(
            Id,
            fileName,
            originalFileName,
            contentType,
            sizeInBytes,
            sha256Hash,
            storageProvider,
            objectKey,
            uploadedByUserId,
            photoExifLocation,
            photoTakenAt);

        _attachments.Add(attachment);
        if (PhotoExifLocation is null && photoExifLocation is not null)
        {
            PhotoExifLocation = photoExifLocation;
            PhotoExifGeometry = photoExifLocation.ToWktPoint();
        }

        Touch();

        return attachment;
    }

    public void MarkMunicipalitySyncAttempted()
    {
        LastSyncAttemptAt = DateTimeOffset.UtcNow;
        Touch();
    }

    public void MarkMunicipalitySynced(string externalMunicipalityComplaintId, string? externalMunicipalityStatus = null)
    {
        ExternalMunicipalityComplaintId = Guard.AgainstEmpty(externalMunicipalityComplaintId, nameof(externalMunicipalityComplaintId), 128);
        ExternalMunicipalityStatus = string.IsNullOrWhiteSpace(externalMunicipalityStatus) ? null : externalMunicipalityStatus.Trim();
        SyncedAt = DateTimeOffset.UtcNow;
        LastSyncAttemptAt = SyncedAt;
        Touch();
    }
}
