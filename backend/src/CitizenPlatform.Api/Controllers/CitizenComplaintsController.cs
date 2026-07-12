using CitizenPlatform.Api.Authorization;
using CitizenPlatform.Api.Extensions;
using CitizenPlatform.Api.Models;
using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common.Models;
using CitizenPlatform.Application.DTOs;
using CitizenPlatform.Application.Features.CitizenAccounts;
using CitizenPlatform.Application.Features.Complaints;
using CitizenPlatform.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CitizenPlatform.Api.Controllers;

[ApiController]
[Route("api/citizen/complaints")]
[Authorize(Policy = AuthorizationPolicyNames.RequireCitizen)]
public sealed class CitizenComplaintsController : ControllerBase
{
    private readonly CitizenComplaintListQueryHandler _listHandler;
    private readonly CreateComplaintCommandHandler _createHandler;
    private readonly ICitizenRepository _citizenRepository;
    private readonly ICurrentUserService _currentUser;

    public CitizenComplaintsController(
        CitizenComplaintListQueryHandler listHandler,
        CreateComplaintCommandHandler createHandler,
        ICitizenRepository citizenRepository,
        ICurrentUserService currentUser)
    {
        _listHandler = listHandler;
        _createHandler = createHandler;
        _citizenRepository = citizenRepository;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CitizenComplaintListItemDto>>>> MyComplaints(
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserGuid is not Guid userId)
        {
            return Unauthorized(ApiResponse<IReadOnlyList<CitizenComplaintListItemDto>>.Fail("Geçersiz oturum."));
        }

        var complaints = await _listHandler.HandleAsync(userId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CitizenComplaintListItemDto>>.Ok(complaints));
    }

    [HttpPost]
    [Consumes("application/json")]
    [EnableRateLimiting(RateLimitingPolicyNames.PublicWrite)]
    public async Task<ActionResult<ApiResponse<CreateComplaintResponseDto>>> Create(
        [FromBody] CitizenComplaintCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserGuid is not Guid userId)
        {
            return Unauthorized(ApiResponse<CreateComplaintResponseDto>.Fail("Geçersiz oturum."));
        }

        var citizen = await _citizenRepository.GetByUserIdAsync(userId, cancellationToken);
        if (citizen is null)
        {
            return NotFound(ApiResponse<CreateComplaintResponseDto>.Fail("Vatandaş profili bulunamadı."));
        }

        var command = new CreateComplaintCommand(
            request.CategoryId,
            request.Title,
            request.Description,
            CitizenFullName: null,
            CitizenPhoneNumber: null,
            CitizenEmail: null,
            request.Latitude,
            request.Longitude,
            request.AddressText,
            IsAnonymous: false,
            ComplaintSource.CitizenWeb,
            Attachments: null,
            RegisteredCitizenId: citizen.Id);

        var result = await _createHandler.HandleAsync(command, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return BadRequest(ApiResponse<CreateComplaintResponseDto>.Fail(
                result.Error ?? "Bildirim oluşturulamadı.",
                result.Errors));
        }

        return StatusCode(StatusCodes.Status201Created, ApiResponse<CreateComplaintResponseDto>.Ok(result.Value));
    }
}
