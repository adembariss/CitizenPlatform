using CitizenPlatform.Domain.Common;
using CitizenPlatform.Domain.Enums;

namespace CitizenPlatform.Domain.Entities;

public sealed class Notification : AuditableEntity
{
    private Notification()
    {
    }

    private Notification(
        Guid id,
        Guid municipalityId,
        Guid? userId,
        Guid? citizenId,
        NotificationChannel channel,
        string recipient,
        string subject,
        string body)
        : base(id)
    {
        MunicipalityId = Guard.AgainstEmpty(municipalityId, nameof(municipalityId));
        UserId = userId == Guid.Empty ? null : userId;
        CitizenId = citizenId == Guid.Empty ? null : citizenId;
        Channel = channel;
        Recipient = Guard.AgainstEmpty(recipient, nameof(recipient), 320);
        Subject = Guard.AgainstEmpty(subject, nameof(subject), 200);
        Body = Guard.AgainstEmpty(body, nameof(body), 4000);
        Status = NotificationStatus.Pending;
    }

    public Guid MunicipalityId { get; private set; }

    public Guid? UserId { get; private set; }

    public Guid? CitizenId { get; private set; }

    public NotificationChannel Channel { get; private set; }

    public NotificationStatus Status { get; private set; }

    public string Recipient { get; private set; } = string.Empty;

    public string Subject { get; private set; } = string.Empty;

    public string Body { get; private set; } = string.Empty;

    public DateTimeOffset? SentAt { get; private set; }

    public string? FailureReason { get; private set; }

    public static Notification Create(
        Guid municipalityId,
        Guid? userId,
        Guid? citizenId,
        NotificationChannel channel,
        string recipient,
        string subject,
        string body)
    {
        return new Notification(Guid.NewGuid(), municipalityId, userId, citizenId, channel, recipient, subject, body);
    }

    public void MarkSent()
    {
        Status = NotificationStatus.Sent;
        SentAt = DateTimeOffset.UtcNow;
        FailureReason = null;
        Touch();
    }

    public void MarkFailed(string failureReason)
    {
        Status = NotificationStatus.Failed;
        FailureReason = Guard.AgainstEmpty(failureReason, nameof(failureReason), 4000);
        Touch();
    }
}
