using System.Text.Json;
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
    private readonly IValidator<CreateComplaintCommand> _validator;
    private readonly IGeoMunicipalityResolver _geoMunicipalityResolver;
    private readonly IComplaintCategoryRepository _categoryRepository;
    private readonly ICategoryDepartmentRuleRepository _categoryDepartmentRuleRepository;
    private readonly ICitizenRepository _citizenRepository;
    private readonly IComplaintRepository _complaintRepository;
    private readonly IIntegrationOutboxRepository _integrationOutboxRepository;
    private readonly ITrackingCodeGenerator _trackingCodeGenerator;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public CreateComplaintCommandHandler(
        IValidator<CreateComplaintCommand> validator,
        IGeoMunicipalityResolver geoMunicipalityResolver,
        IComplaintCategoryRepository categoryRepository,
        ICategoryDepartmentRuleRepository categoryDepartmentRuleRepository,
        ICitizenRepository citizenRepository,
        IComplaintRepository complaintRepository,
        IIntegrationOutboxRepository integrationOutboxRepository,
        ITrackingCodeGenerator trackingCodeGenerator,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _validator = validator;
        _geoMunicipalityResolver = geoMunicipalityResolver;
        _categoryRepository = categoryRepository;
        _categoryDepartmentRuleRepository = categoryDepartmentRuleRepository;
        _citizenRepository = citizenRepository;
        _complaintRepository = complaintRepository;
        _integrationOutboxRepository = integrationOutboxRepository;
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

        var municipalityResult = await _geoMunicipalityResolver.ResolveByCoordinateAsync(
            command.Latitude,
            command.Longitude,
            cancellationToken);

        if (!municipalityResult.IsSuccess || municipalityResult.MunicipalityId is null)
        {
            return Result<CreateComplaintResponseDto>.Failure(
                municipalityResult.FailureReason ?? "Municipality could not be resolved.");
        }

        var municipalityId = municipalityResult.MunicipalityId.Value;
        var category = await _categoryRepository.GetActiveForMunicipalityAsync(
            command.CategoryId,
            municipalityId,
            cancellationToken);

        if (category is null)
        {
            return Result<CreateComplaintResponseDto>.Failure("Complaint category is not active or not available for the municipality.");
        }

        return await _unitOfWork.ExecuteInTransactionAsync(
            async ct => await CreateComplaintAsync(command, municipalityResult, category, ct),
            cancellationToken);
    }

    private async Task<Result<CreateComplaintResponseDto>> CreateComplaintAsync(
        CreateComplaintCommand command,
        MunicipalityResolveResult municipalityResult,
        ComplaintCategory category,
        CancellationToken cancellationToken)
    {
        var municipalityId = municipalityResult.MunicipalityId!.Value;
        var citizen = BuildCitizen(command);
        if (citizen is not null)
        {
            await _citizenRepository.AddAsync(citizen, cancellationToken);
        }

        var rule = await _categoryDepartmentRuleRepository.GetActiveRuleAsync(
            municipalityId,
            category.Id,
            cancellationToken);

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

        if (rule is not null)
        {
            complaint.RouteToDepartment(rule.DepartmentId);
        }

        complaint.RecordInitialStatus("Complaint created.");

        await _complaintRepository.AddAsync(complaint, cancellationToken);
        await _integrationOutboxRepository.AddAsync(
            BuildComplaintCreatedOutboxMessage(complaint, municipalityResult, category, citizen),
            cancellationToken);

        var response = new CreateComplaintResponseDto(
            complaint.Id,
            complaint.TrackingCode,
            municipalityResult.MunicipalityName ?? string.Empty,
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
        MunicipalityResolveResult municipalityResult,
        ComplaintCategory category,
        Citizen? citizen)
    {
        var payload = JsonSerializer.Serialize(new
        {
            EventType = "ComplaintCreated",
            ComplaintId = complaint.Id,
            complaint.TrackingCode,
            complaint.MunicipalityId,
            MunicipalityName = municipalityResult.MunicipalityName,
            MunicipalityCode = municipalityResult.MunicipalityCode,
            complaint.CategoryId,
            CategoryCode = category.Code,
            complaint.CurrentDepartmentId,
            complaint.Title,
            complaint.Description,
            complaint.AddressText,
            complaint.Status,
            complaint.Priority,
            complaint.Source,
            Latitude = complaint.Location.Latitude,
            Longitude = complaint.Location.Longitude,
            CitizenId = citizen?.Id,
            CreatedAt = complaint.CreatedAt
        });

        return IntegrationOutboxMessage.Create(
            complaint.MunicipalityId,
            complaint.Id,
            nameof(Complaint),
            "ComplaintCreated",
            payload);
    }
}
