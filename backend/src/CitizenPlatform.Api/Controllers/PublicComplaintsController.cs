using CitizenPlatform.Application.Common.Models;
using CitizenPlatform.Application.DTOs;
using CitizenPlatform.Application.Features.Complaints;
using Microsoft.AspNetCore.Mvc;

namespace CitizenPlatform.Api.Controllers;

[ApiController]
[Route("api/public/complaints")]
public sealed class PublicComplaintsController : ControllerBase
{
    private readonly CreateComplaintCommandHandler _handler;

    public PublicComplaintsController(CreateComplaintCommandHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CreateComplaintResponseDto>>> Create(
        [FromBody] CreateComplaintRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new CreateComplaintCommand(
            request.CategoryId,
            request.Title,
            request.Description,
            request.CitizenFullName,
            request.CitizenPhoneNumber,
            request.CitizenEmail,
            request.Latitude,
            request.Longitude,
            request.AddressText,
            request.IsAnonymous,
            request.Source);

        var result = await _handler.HandleAsync(command, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return BadRequest(ApiResponse<CreateComplaintResponseDto>.Fail(
                result.Error ?? "Complaint could not be created.",
                result.Errors));
        }

        return StatusCode(StatusCodes.Status201Created, ApiResponse<CreateComplaintResponseDto>.Ok(result.Value));
    }
}
