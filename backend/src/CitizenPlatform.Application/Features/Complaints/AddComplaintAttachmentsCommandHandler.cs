using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common.Models;
using CitizenPlatform.Application.DTOs;
using FluentValidation;

namespace CitizenPlatform.Application.Features.Complaints;

public sealed class AddComplaintAttachmentsCommandHandler
{
    private static readonly TimeSpan AttachmentUploadWindow = TimeSpan.FromMinutes(30);

    private readonly IValidator<AddComplaintAttachmentsCommand> _validator;
    private readonly IComplaintRepository _complaintRepository;
    private readonly ComplaintAttachmentUploadService _attachmentUploadService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public AddComplaintAttachmentsCommandHandler(
        IValidator<AddComplaintAttachmentsCommand> validator,
        IComplaintRepository complaintRepository,
        ComplaintAttachmentUploadService attachmentUploadService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _validator = validator;
        _complaintRepository = complaintRepository;
        _attachmentUploadService = attachmentUploadService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AddComplaintAttachmentsResponseDto>> HandleAsync(
        AddComplaintAttachmentsCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<AddComplaintAttachmentsResponseDto>.Failure(
                "Validation failed.",
                validationResult.Errors.Select(error => error.ErrorMessage).ToArray());
        }

        return await _unitOfWork.ExecuteInTransactionAsync(
            async ct => await AddAttachmentsAsync(command, ct),
            cancellationToken);
    }

    private async Task<Result<AddComplaintAttachmentsResponseDto>> AddAttachmentsAsync(
        AddComplaintAttachmentsCommand command,
        CancellationToken cancellationToken)
    {
        var complaint = await _complaintRepository.GetByTrackingCodeAsync(command.TrackingCode, cancellationToken);
        if (complaint is null)
        {
            return Result<AddComplaintAttachmentsResponseDto>.Failure("Complaint could not be found.");
        }

        if (_dateTimeProvider.UtcNow - complaint.CreatedAt > AttachmentUploadWindow)
        {
            return Result<AddComplaintAttachmentsResponseDto>.Failure("Attachment upload window has expired.");
        }

        var attachmentResult = await _attachmentUploadService.AddUploadsAsync(
            complaint,
            command.Attachments,
            cancellationToken);

        if (!attachmentResult.IsSuccess || attachmentResult.Value is null)
        {
            return Result<AddComplaintAttachmentsResponseDto>.Failure(
                attachmentResult.Error ?? "Attachments could not be added.",
                attachmentResult.Errors);
        }

        return Result<AddComplaintAttachmentsResponseDto>.Success(
            new AddComplaintAttachmentsResponseDto(
                complaint.Id,
                complaint.TrackingCode,
                attachmentResult.Value));
    }
}
