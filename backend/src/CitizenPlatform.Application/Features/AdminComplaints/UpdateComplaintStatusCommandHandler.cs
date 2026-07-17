using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common;
using CitizenPlatform.Application.Common.Models;
using CitizenPlatform.Application.DTOs;
using CitizenPlatform.Domain.Entities;

namespace CitizenPlatform.Application.Features.AdminComplaints;

public sealed class UpdateComplaintStatusCommandHandler
{
    private readonly IComplaintRepository _complaintRepository;
    private readonly IIntegrationOutboxRepository _outboxRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateComplaintStatusCommandHandler(
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

    public async Task<AdminScopedResult<ComplaintDto>> HandleAsync(
        UpdateComplaintStatusCommand command,
        TenantScope scope,
        CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(
            ct => ExecuteAsync(command, scope, ct),
            cancellationToken);
    }

    private async Task<AdminScopedResult<ComplaintDto>> ExecuteAsync(
        UpdateComplaintStatusCommand command,
        TenantScope scope,
        CancellationToken cancellationToken)
    {
        var complaint = await _complaintRepository.GetByIdAsync(command.ComplaintId, cancellationToken);
        if (complaint is null || !scope.CanAccessComplaint(complaint.MunicipalityId, complaint.InstitutionId))
        {
            return AdminScopedResult<ComplaintDto>.AsNotFound();
        }

        if (complaint.Status == command.NewStatus)
        {
            return AdminScopedResult<ComplaintDto>.Failure("Complaint is already in the requested status.");
        }

        var previousStatus = complaint.Status;
        complaint.ChangeStatus(command.NewStatus, command.ChangedByUserId, command.Note, command.IsVisibleToCitizen);

        var outboxMessage = IntegrationOutboxMessage.Create(
            complaint.MunicipalityId,
            complaint.Id,
            nameof(Complaint),
            "ComplaintStatusChanged",
            SerializeStatusChangedPayload(complaint, previousStatus, command));

        await _outboxRepository.AddAsync(outboxMessage, cancellationToken);

        return AdminScopedResult<ComplaintDto>.Success(ToDto(complaint));
    }

    private string SerializeStatusChangedPayload(
        Complaint complaint,
        Domain.Enums.ComplaintStatus previousStatus,
        UpdateComplaintStatusCommand command)
    {
        var payload = new ComplaintStatusChangedPayload(
            complaint.Id,
            complaint.MunicipalityId,
            complaint.TrackingCode,
            previousStatus.ToString(),
            command.NewStatus.ToString(),
            command.Note,
            command.IsVisibleToCitizen,
            command.ChangedByUserId,
            _dateTimeProvider.UtcNow);

        return System.Text.Json.JsonSerializer.Serialize(payload, OutboxJson.Options);
    }

    private static ComplaintDto ToDto(Complaint complaint)
    {
        return new ComplaintDto(
            complaint.Id,
            complaint.MunicipalityId,
            complaint.CategoryId,
            complaint.TrackingCode,
            complaint.Title,
            complaint.Description,
            complaint.Location.Latitude,
            complaint.Location.Longitude,
            complaint.Status,
            complaint.Priority,
            complaint.Source,
            complaint.CreatedAt);
    }
}
