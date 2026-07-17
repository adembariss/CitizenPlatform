using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common;
using CitizenPlatform.Application.Common.Models;
using CitizenPlatform.Application.DTOs;
using CitizenPlatform.Domain.Entities;

namespace CitizenPlatform.Application.Features.AdminComplaints;

public sealed class AddAdminCommentCommandHandler
{
    private readonly IComplaintRepository _complaintRepository;
    private readonly IIntegrationOutboxRepository _outboxRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public AddAdminCommentCommandHandler(
        IComplaintRepository complaintRepository,
        IIntegrationOutboxRepository outboxRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _complaintRepository = complaintRepository;
        _outboxRepository = outboxRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<AdminScopedResult<AdminComplaintCommentDto>> HandleAsync(
        AddAdminCommentCommand command,
        TenantScope scope,
        CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(
            ct => ExecuteAsync(command, scope, ct),
            cancellationToken);
    }

    private async Task<AdminScopedResult<AdminComplaintCommentDto>> ExecuteAsync(
        AddAdminCommentCommand command,
        TenantScope scope,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.CommentText))
        {
            return AdminScopedResult<AdminComplaintCommentDto>.Failure("Comment text is required.");
        }

        var complaint = await _complaintRepository.GetByIdAsync(command.ComplaintId, cancellationToken);
        if (complaint is null || !scope.CanAccessComplaint(complaint.MunicipalityId, complaint.InstitutionId))
        {
            return AdminScopedResult<AdminComplaintCommentDto>.AsNotFound();
        }

        var comment = complaint.AddComment(command.AuthorUserId, command.CommentText, command.IsInternal);

        var payload = new AdminCommentAddedPayload(
            complaint.Id,
            complaint.MunicipalityId,
            complaint.TrackingCode,
            command.CommentText,
            command.IsInternal,
            command.AuthorUserId,
            _dateTimeProvider.UtcNow);

        var outboxMessage = IntegrationOutboxMessage.Create(
            complaint.MunicipalityId,
            complaint.Id,
            nameof(Complaint),
            "AdminCommentAdded",
            System.Text.Json.JsonSerializer.Serialize(payload, OutboxJson.Options));

        await _outboxRepository.AddAsync(outboxMessage, cancellationToken);

        return AdminScopedResult<AdminComplaintCommentDto>.Success(
            AdminComplaintDetailQueryHandler.MapComment(comment));
    }
}
