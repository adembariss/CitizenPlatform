using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common;
using CitizenPlatform.Application.Common.Models;
using CitizenPlatform.Application.DTOs;
using CitizenPlatform.Domain.Entities;

namespace CitizenPlatform.Application.Features.AdminComplaints;

public sealed class AssignComplaintCommandHandler
{
    private readonly IComplaintRepository _complaintRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IIntegrationOutboxRepository _outboxRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public AssignComplaintCommandHandler(
        IComplaintRepository complaintRepository,
        IDepartmentRepository departmentRepository,
        IIntegrationOutboxRepository outboxRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _complaintRepository = complaintRepository;
        _departmentRepository = departmentRepository;
        _outboxRepository = outboxRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<AdminScopedResult<ComplaintDto>> HandleAsync(
        AssignComplaintCommand command,
        TenantScope scope,
        CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(
            ct => ExecuteAsync(command, scope, ct),
            cancellationToken);
    }

    private async Task<AdminScopedResult<ComplaintDto>> ExecuteAsync(
        AssignComplaintCommand command,
        TenantScope scope,
        CancellationToken cancellationToken)
    {
        var complaint = await _complaintRepository.GetByIdAsync(command.ComplaintId, cancellationToken);
        if (complaint is null || !scope.CanAccess(complaint.MunicipalityId))
        {
            return AdminScopedResult<ComplaintDto>.AsNotFound();
        }

        var department = await _departmentRepository.GetByIdAsync(command.DepartmentId, cancellationToken);
        if (department is null || department.MunicipalityId != complaint.MunicipalityId)
        {
            return AdminScopedResult<ComplaintDto>.Failure("Department does not belong to this municipality.");
        }

        complaint.AssignToDepartment(command.DepartmentId, command.AssignedByUserId, command.AssignedUserId, command.Note);

        var payload = new ComplaintAssignedPayload(
            complaint.Id,
            complaint.MunicipalityId,
            complaint.TrackingCode,
            department.Id,
            department.Name,
            command.AssignedUserId,
            command.Note,
            command.AssignedByUserId,
            _dateTimeProvider.UtcNow);

        var outboxMessage = IntegrationOutboxMessage.Create(
            complaint.MunicipalityId,
            complaint.Id,
            nameof(Complaint),
            "ComplaintAssigned",
            System.Text.Json.JsonSerializer.Serialize(payload, OutboxJson.Options));

        await _outboxRepository.AddAsync(outboxMessage, cancellationToken);

        return AdminScopedResult<ComplaintDto>.Success(ToDto(complaint));
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
