using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common;

namespace CitizenPlatform.Application.Features.AdminComplaints;

public sealed record AdminComplaintAttachmentFile(
    Stream Content,
    string ContentType,
    string OriginalFileName,
    long SizeInBytes,
    string Sha256Hash);

public sealed class AdminComplaintAttachmentQueryHandler
{
    private readonly IAdminComplaintQueryRepository _repository;
    private readonly IFileStorageService _fileStorageService;

    public AdminComplaintAttachmentQueryHandler(
        IAdminComplaintQueryRepository repository,
        IFileStorageService fileStorageService)
    {
        _repository = repository;
        _fileStorageService = fileStorageService;
    }

    public async Task<AdminComplaintAttachmentFile?> HandleAsync(
        Guid complaintId,
        Guid attachmentId,
        TenantScope scope,
        CancellationToken cancellationToken)
    {
        if (!scope.IsAuthorizedAdmin)
        {
            return null;
        }

        var row = await _repository.GetDetailAsync(complaintId, null, cancellationToken);
        if (row is null || !scope.CanAccessComplaint(row.Complaint.MunicipalityId, row.Complaint.InstitutionId))
        {
            return null;
        }

        var attachment = row.Complaint.Attachments.SingleOrDefault(item => item.Id == attachmentId);
        if (attachment is null)
        {
            return null;
        }

        Stream content;
        try
        {
            content = await _fileStorageService.OpenReadAsync(attachment.ObjectKey, cancellationToken);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (DirectoryNotFoundException)
        {
            return null;
        }

        return new AdminComplaintAttachmentFile(
            content,
            attachment.ContentType,
            attachment.OriginalFileName,
            attachment.SizeInBytes,
            attachment.Sha256Hash);
    }
}
