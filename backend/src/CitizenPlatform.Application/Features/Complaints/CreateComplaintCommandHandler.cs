using System.Text.Json;
using System.Text.Json.Serialization;
using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common.Models;
using CitizenPlatform.Application.DTOs;
using CitizenPlatform.Domain.Entities;
using CitizenPlatform.Domain.Enums;
using CitizenPlatform.Domain.ValueObjects;
using FluentValidation;

namespace CitizenPlatform.Application.Features.Complaints;

public sealed class CreateComplaintCommandHandler
{
    private static readonly JsonSerializerOptions OutboxJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IValidator<CreateComplaintCommand> _validator;
    private readonly IGeoMunicipalityResolver _geoMunicipalityResolver;
    private readonly IMunicipalityRepository _municipalityRepository;
    private readonly IInstitutionRepository _institutionRepository;
    private readonly IComplaintCategoryRepository _categoryRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly ICategoryDepartmentRuleRepository _categoryDepartmentRuleRepository;
    private readonly ICitizenRepository _citizenRepository;
    private readonly IComplaintRepository _complaintRepository;
    private readonly IIntegrationOutboxRepository _integrationOutboxRepository;
    private readonly ComplaintAttachmentUploadService _attachmentUploadService;
    private readonly ITrackingCodeGenerator _trackingCodeGenerator;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public CreateComplaintCommandHandler(
        IValidator<CreateComplaintCommand> validator,
        IGeoMunicipalityResolver geoMunicipalityResolver,
        IMunicipalityRepository municipalityRepository,
        IInstitutionRepository institutionRepository,
        IComplaintCategoryRepository categoryRepository,
        IDepartmentRepository departmentRepository,
        ICategoryDepartmentRuleRepository categoryDepartmentRuleRepository,
        ICitizenRepository citizenRepository,
        IComplaintRepository complaintRepository,
        IIntegrationOutboxRepository integrationOutboxRepository,
        ComplaintAttachmentUploadService attachmentUploadService,
        ITrackingCodeGenerator trackingCodeGenerator,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _validator = validator;
        _geoMunicipalityResolver = geoMunicipalityResolver;
        _municipalityRepository = municipalityRepository;
        _institutionRepository = institutionRepository;
        _categoryRepository = categoryRepository;
        _departmentRepository = departmentRepository;
        _categoryDepartmentRuleRepository = categoryDepartmentRuleRepository;
        _citizenRepository = citizenRepository;
        _complaintRepository = complaintRepository;
        _integrationOutboxRepository = integrationOutboxRepository;
        _attachmentUploadService = attachmentUploadService;
        _trackingCodeGenerator = trackingCodeGenerator;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CreateComplaintResponseDto>> HandleAsync(
        CreateComplaintCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<CreateComplaintResponseDto>.Failure(
                "Validation failed.",
                validationResult.Errors.Select(error => error.ErrorMessage).ToArray());
        }

        MunicipalityResolveResult municipalityResult;
        if (command.MunicipalityId is Guid selectedMunicipalityId)
        {
            // Address selector: the citizen picked the municipality directly.
            var municipality = await _municipalityRepository.GetByIdAsync(selectedMunicipalityId, cancellationToken);
            if (municipality is null || !municipality.IsActive)
            {
                return Result<CreateComplaintResponseDto>.Failure("Seçilen belediye bulunamadı.");
            }

            municipalityResult = MunicipalityResolveResult.Success(municipality.Id, municipality.Name, municipality.Code);
        }
        else
        {
            municipalityResult = await _geoMunicipalityResolver.ResolveByCoordinateAsync(
                command.Latitude,
                command.Longitude,
                cancellationToken);

            if (!municipalityResult.IsSuccess || municipalityResult.MunicipalityId is null)
            {
                return Result<CreateComplaintResponseDto>.Failure(
                    municipalityResult.FailureReason ?? "Municipality could not be resolved.");
            }
        }

        var municipalityId = municipalityResult.MunicipalityId.Value;

        // Şikayet bir dağıtım kurumuna (elektrik/su/doğalgaz) yönlendiriliyorsa kategori o kuruma ait
        // olmalı; belediye konumu (municipalityId) yine kayıt için tutulur.
        Institution? institution = null;
        ComplaintCategory? category;
        if (command.InstitutionId is Guid targetInstitutionId)
        {
            institution = await _institutionRepository.GetByIdAsync(targetInstitutionId, cancellationToken);
            if (institution is null || !institution.IsActive)
            {
                return Result<CreateComplaintResponseDto>.Failure("Seçilen kurum bulunamadı.");
            }

            category = await _categoryRepository.GetActiveForInstitutionAsync(command.CategoryId, targetInstitutionId, cancellationToken);
            if (category is null)
            {
                return Result<CreateComplaintResponseDto>.Failure("Seçilen kategori bu kurum için geçerli değil.");
            }
        }
        else
        {
            category = await _categoryRepository.GetActiveForMunicipalityAsync(command.CategoryId, municipalityId, cancellationToken);
            if (category is null)
            {
                return Result<CreateComplaintResponseDto>.Failure("Complaint category is not active or not available for the municipality.");
            }
        }

        return await _unitOfWork.ExecuteInTransactionAsync(
            async ct => await CreateComplaintAsync(command, municipalityResult, category, institution, ct),
            cancellationToken);
    }

    private async Task<Result<CreateComplaintResponseDto>> CreateComplaintAsync(
        CreateComplaintCommand command,
        MunicipalityResolveResult municipalityResult,
        ComplaintCategory category,
        Institution? institution,
        CancellationToken cancellationToken)
    {
        var municipalityId = municipalityResult.MunicipalityId!.Value;

        Citizen? citizen;
        if (command.RegisteredCitizenId is Guid registeredCitizenId)
        {
            // Authenticated citizen: reuse their existing profile, do not create a new record.
            citizen = await _citizenRepository.GetByIdAsync(registeredCitizenId, cancellationToken);
        }
        else
        {
            citizen = BuildCitizen(command);
            if (citizen is not null)
            {
                await _citizenRepository.AddAsync(citizen, cancellationToken);
            }
        }

        var rule = await _categoryDepartmentRuleRepository.GetActiveRuleAsync(
            municipalityId,
            category.Id,
            cancellationToken);
        Department? department = null;

        var trackingCode = await GenerateUniqueTrackingCodeAsync(cancellationToken);
        if (trackingCode is null)
        {
            return Result<CreateComplaintResponseDto>.Failure("Could not generate a unique tracking code.");
        }

        var createdAt = _dateTimeProvider.UtcNow;
        var complaint = Complaint.Create(
            municipalityId,
            category.Id,
            trackingCode,
            BuildTitle(command.Title, command.Description),
            command.Description,
            new GeoCoordinate(command.Latitude, command.Longitude),
            command.Source,
            citizen?.Id,
            priority: rule?.DefaultPriority ?? ComplaintPriority.Normal,
            addressText: command.AddressText,
            createdAt: createdAt);

        if (institution is not null)
        {
            complaint.SetTargetInstitution(institution.Id);
        }
        else if (rule is not null)
        {
            complaint.RouteToDepartment(rule.DepartmentId);
            department = await _departmentRepository.GetByIdAsync(rule.DepartmentId, cancellationToken);
        }

        complaint.RecordInitialStatus("Complaint created.");

        var attachmentResult = await _attachmentUploadService.AddUploadsAsync(
            complaint,
            command.Attachments,
            cancellationToken);

        if (!attachmentResult.IsSuccess)
        {
            return Result<CreateComplaintResponseDto>.Failure(
                attachmentResult.Error ?? "Attachments could not be added.",
                attachmentResult.Errors);
        }

        await _complaintRepository.AddAsync(complaint, cancellationToken);

        // Belediye şikayetleri belediyenin dış sistemine (outbox → worker) senkronlanır.
        // Kurum şikayetlerinin böyle bir dış hedefi yoktur; outbox atlanır.
        if (institution is null)
        {
            await _integrationOutboxRepository.AddAsync(
                BuildComplaintCreatedOutboxMessage(complaint, category, department, citizen),
                cancellationToken);
        }

        var response = new CreateComplaintResponseDto(
            complaint.Id,
            complaint.TrackingCode,
            institution?.Name ?? municipalityResult.MunicipalityName ?? string.Empty,
            complaint.Status,
            complaint.CreatedAt);

        return Result<CreateComplaintResponseDto>.Success(response);
    }

    private async Task<string?> GenerateUniqueTrackingCodeAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var trackingCode = await _trackingCodeGenerator.GenerateAsync(cancellationToken);
            if (!await _complaintRepository.ExistsByTrackingCodeAsync(trackingCode, cancellationToken))
            {
                return trackingCode;
            }
        }

        return null;
    }

    private static Citizen? BuildCitizen(CreateComplaintCommand command)
    {
        if (command.IsAnonymous || !HasCitizenMetadata(command))
        {
            return null;
        }

        var fullName = string.IsNullOrWhiteSpace(command.CitizenFullName)
            ? "Vatandas"
            : command.CitizenFullName;

        return Citizen.Create(null, fullName, command.CitizenPhoneNumber, command.CitizenEmail);
    }

    private static bool HasCitizenMetadata(CreateComplaintCommand command)
    {
        return !string.IsNullOrWhiteSpace(command.CitizenFullName)
            || !string.IsNullOrWhiteSpace(command.CitizenPhoneNumber)
            || !string.IsNullOrWhiteSpace(command.CitizenEmail);
    }

    private static string BuildTitle(string? title, string description)
    {
        if (!string.IsNullOrWhiteSpace(title))
        {
            return title.Trim();
        }

        var normalizedDescription = description.Trim();
        return normalizedDescription.Length <= 120
            ? normalizedDescription
            : normalizedDescription[..120];
    }

    private static IntegrationOutboxMessage BuildComplaintCreatedOutboxMessage(
        Complaint complaint,
        ComplaintCategory category,
        Department? department,
        Citizen? citizen)
    {
        var payload = JsonSerializer.Serialize(
            new MunicipalityComplaintCreatedPayload(
                complaint.Id,
                complaint.MunicipalityId,
                complaint.TrackingCode,
                category.Name,
                department?.Name,
                citizen?.FullName,
                citizen?.PhoneNumber,
                citizen?.Email,
                complaint.Description,
                complaint.AddressText,
                complaint.Location.Latitude,
                complaint.Location.Longitude,
                complaint.Status.ToString(),
                complaint.Priority.ToString(),
                complaint.CreatedAt),
            OutboxJsonOptions);

        return IntegrationOutboxMessage.Create(
            complaint.MunicipalityId,
            complaint.Id,
            nameof(Complaint),
            "ComplaintCreated",
            payload);
    }
}
