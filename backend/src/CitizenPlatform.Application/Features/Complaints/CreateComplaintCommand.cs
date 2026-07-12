using CitizenPlatform.Domain.Enums;
using CitizenPlatform.Application.DTOs;

namespace CitizenPlatform.Application.Features.Complaints;

public sealed record CreateComplaintCommand(
    Guid CategoryId,
    string? Title,
    string Description,
    string? CitizenFullName,
    string? CitizenPhoneNumber,
    string? CitizenEmail,
    double Latitude,
    double Longitude,
    string? AddressText,
    bool IsAnonymous,
    ComplaintSource Source,
    IReadOnlyCollection<ComplaintAttachmentUpload>? Attachments = null,
    // Set when an authenticated citizen submits: the complaint is linked to their
    // existing Citizen record instead of creating a new anonymous contact record.
    Guid? RegisteredCitizenId = null);
