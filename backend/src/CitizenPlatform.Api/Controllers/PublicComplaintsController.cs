using CitizenPlatform.Api.Extensions;
using CitizenPlatform.Application.Common.Models;
using CitizenPlatform.Application.DTOs;
using CitizenPlatform.Application.Features.Complaints;
using CitizenPlatform.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CitizenPlatform.Api.Controllers;

[ApiController]
[Route("api/public/complaints")]
[EnableRateLimiting(RateLimitingPolicyNames.PublicWrite)]
public sealed class PublicComplaintsController : ControllerBase
{
    private readonly CreateComplaintCommandHandler _createComplaintHandler;
    private readonly AddComplaintAttachmentsCommandHandler _addComplaintAttachmentsHandler;
    private readonly TrackComplaintQueryHandler _trackComplaintHandler;

    public PublicComplaintsController(
        CreateComplaintCommandHandler createComplaintHandler,
        AddComplaintAttachmentsCommandHandler addComplaintAttachmentsHandler,
        TrackComplaintQueryHandler trackComplaintHandler)
    {
        _createComplaintHandler = createComplaintHandler;
        _addComplaintAttachmentsHandler = addComplaintAttachmentsHandler;
        _trackComplaintHandler = trackComplaintHandler;
    }

    [HttpPost]
    [Consumes("application/json")]
    public async Task<ActionResult<ApiResponse<CreateComplaintResponseDto>>> CreateJson(
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

        var result = await _createComplaintHandler.HandleAsync(command, cancellationToken);
        return ToCreateResponse(result);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<CreateComplaintResponseDto>>> CreateMultipart(
        [FromForm] CreateComplaintMultipartRequest request,
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
            request.Source,
            MapFiles(Request.Form.Files));

        var result = await _createComplaintHandler.HandleAsync(command, cancellationToken);
        return ToCreateResponse(result);
    }

    [HttpGet("track/{trackingCode}")]
    [EnableRateLimiting(RateLimitingPolicyNames.PublicRead)]
    public async Task<ActionResult<ApiResponse<PublicComplaintTrackingDto>>> Track(
        string trackingCode,
        CancellationToken cancellationToken)
    {
        var result = await _trackComplaintHandler.HandleAsync(trackingCode, cancellationToken);
        if (result is null)
        {
            return NotFound(ApiResponse<PublicComplaintTrackingDto>.Fail("Complaint could not be found."));
        }

        return Ok(ApiResponse<PublicComplaintTrackingDto>.Ok(result));
    }

    [HttpPost("{trackingCode}/attachments")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<AddComplaintAttachmentsResponseDto>>> AddAttachments(
        string trackingCode,
        CancellationToken cancellationToken)
    {
        var command = new AddComplaintAttachmentsCommand(
            trackingCode,
            MapFiles(Request.Form.Files));

        var result = await _addComplaintAttachmentsHandler.HandleAsync(command, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return BadRequest(ApiResponse<AddComplaintAttachmentsResponseDto>.Fail(
                result.Error ?? "Attachments could not be added.",
                result.Errors));
        }

        return StatusCode(StatusCodes.Status201Created, ApiResponse<AddComplaintAttachmentsResponseDto>.Ok(result.Value));
    }

    private static ActionResult<ApiResponse<CreateComplaintResponseDto>> ToCreateResponse(
        Result<CreateComplaintResponseDto> result)
    {
        if (!result.IsSuccess || result.Value is null)
        {
            return new BadRequestObjectResult(ApiResponse<CreateComplaintResponseDto>.Fail(
                result.Error ?? "Complaint could not be created.",
                result.Errors));
        }

        return new ObjectResult(ApiResponse<CreateComplaintResponseDto>.Ok(result.Value))
        {
            StatusCode = StatusCodes.Status201Created
        };
    }

    private static IReadOnlyCollection<ComplaintAttachmentUpload> MapFiles(IFormFileCollection files)
    {
        return files
            .Select(file => new ComplaintAttachmentUpload(
                file.FileName,
                file.ContentType,
                file.Length,
                file.OpenReadStream))
            .ToArray();
    }
}
