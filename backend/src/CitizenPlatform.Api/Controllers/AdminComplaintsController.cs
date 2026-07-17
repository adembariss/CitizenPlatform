using CitizenPlatform.Api.Authorization;
using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common;
using CitizenPlatform.Application.Common.Models;
using CitizenPlatform.Application.DTOs;
using CitizenPlatform.Application.Features.AdminComplaints;
using CitizenPlatform.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CitizenPlatform.Api.Controllers;

[ApiController]
[Route("api/admin/complaints")]
[Authorize(Policy = AuthorizationPolicyNames.RequireAdminAccess)]
public sealed class AdminComplaintsController : ControllerBase
{
    private readonly AdminComplaintListQueryHandler _listHandler;
    private readonly AdminComplaintDetailQueryHandler _detailHandler;
    private readonly AdminComplaintAttachmentQueryHandler _attachmentHandler;
    private readonly AdminComplaintHistoryQueryHandler _historyHandler;
    private readonly UpdateComplaintStatusCommandHandler _updateStatusHandler;
    private readonly AssignComplaintCommandHandler _assignHandler;
    private readonly AddAdminCommentCommandHandler _addCommentHandler;
    private readonly ICurrentUserService _currentUserService;

    public AdminComplaintsController(
        AdminComplaintListQueryHandler listHandler,
        AdminComplaintDetailQueryHandler detailHandler,
        AdminComplaintAttachmentQueryHandler attachmentHandler,
        AdminComplaintHistoryQueryHandler historyHandler,
        UpdateComplaintStatusCommandHandler updateStatusHandler,
        AssignComplaintCommandHandler assignHandler,
        AddAdminCommentCommandHandler addCommentHandler,
        ICurrentUserService currentUserService)
    {
        _listHandler = listHandler;
        _detailHandler = detailHandler;
        _attachmentHandler = attachmentHandler;
        _historyHandler = historyHandler;
        _updateStatusHandler = updateStatusHandler;
        _assignHandler = assignHandler;
        _addCommentHandler = addCommentHandler;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<AdminComplaintListResponseDto>> List(
        [FromQuery] ComplaintStatus? status,
        [FromQuery] Guid? categoryId,
        [FromQuery] Guid? departmentId,
        [FromQuery] DateTimeOffset? dateFrom,
        [FromQuery] DateTimeOffset? dateTo,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? municipalityId = null,
        [FromQuery] string? province = null,
        CancellationToken cancellationToken = default)
    {
        var query = new AdminComplaintListQuery(status, categoryId, departmentId, dateFrom, dateTo, search, page, pageSize, municipalityId, province);
        var response = await _listHandler.HandleAsync(query, TenantScope.From(_currentUserService), cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AdminComplaintDetailDto>>> Detail(Guid id, CancellationToken cancellationToken)
    {
        var detail = await _detailHandler.HandleAsync(id, TenantScope.From(_currentUserService), cancellationToken);
        if (detail is null)
        {
            return NotFound(ApiResponse<AdminComplaintDetailDto>.Fail("Complaint not found."));
        }

        return Ok(ApiResponse<AdminComplaintDetailDto>.Ok(detail));
    }

    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<ApiResponse<AdminComplaintHistoryDto>>> History(Guid id, CancellationToken cancellationToken)
    {
        var history = await _historyHandler.HandleAsync(id, TenantScope.From(_currentUserService), cancellationToken);
        if (history is null)
        {
            return NotFound(ApiResponse<AdminComplaintHistoryDto>.Fail("Complaint not found."));
        }

        return Ok(ApiResponse<AdminComplaintHistoryDto>.Ok(history));
    }

    [HttpGet("{id:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> Attachment(
        Guid id,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        var attachment = await _attachmentHandler.HandleAsync(
            id,
            attachmentId,
            TenantScope.From(_currentUserService),
            cancellationToken);

        if (attachment is null)
        {
            return NotFound(ApiResponse<object>.Fail("Attachment not found."));
        }

        Response.Headers.CacheControl = "private, no-store, max-age=0";
        Response.Headers.ETag = $"\"{attachment.Sha256Hash}\"";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.ContentLength = attachment.SizeInBytes;

        return File(
            attachment.Content,
            attachment.ContentType,
            attachment.OriginalFileName,
            enableRangeProcessing: true);
    }

    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<ComplaintDto>>> UpdateStatus(
        Guid id,
        [FromBody] UpdateComplaintStatusRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActingUserId(out var actingUserId))
        {
            return Unauthorized(ApiResponse<ComplaintDto>.Fail("Invalid token."));
        }

        var command = new UpdateComplaintStatusCommand(id, request.NewStatus, request.Note, request.IsVisibleToCitizen, actingUserId);
        var result = await _updateStatusHandler.HandleAsync(command, TenantScope.From(_currentUserService), cancellationToken);

        return ToActionResult(result);
    }

    [HttpPut("{id:guid}/assign")]
    public async Task<ActionResult<ApiResponse<ComplaintDto>>> Assign(
        Guid id,
        [FromBody] AssignComplaintRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActingUserId(out var actingUserId))
        {
            return Unauthorized(ApiResponse<ComplaintDto>.Fail("Invalid token."));
        }

        var command = new AssignComplaintCommand(id, request.DepartmentId, request.AssignedUserId, request.Note, actingUserId);
        var result = await _assignHandler.HandleAsync(command, TenantScope.From(_currentUserService), cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<ActionResult<ApiResponse<AdminComplaintCommentDto>>> AddComment(
        Guid id,
        [FromBody] AddComplaintCommentRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActingUserId(out var actingUserId))
        {
            return Unauthorized(ApiResponse<AdminComplaintCommentDto>.Fail("Invalid token."));
        }

        var command = new AddAdminCommentCommand(id, request.CommentText, request.IsInternal, actingUserId);
        var result = await _addCommentHandler.HandleAsync(command, TenantScope.From(_currentUserService), cancellationToken);

        return ToActionResult(result);
    }

    private bool TryGetActingUserId(out Guid userId)
    {
        userId = _currentUserService.UserGuid ?? Guid.Empty;
        return userId != Guid.Empty;
    }

    private ActionResult<ApiResponse<T>> ToActionResult<T>(AdminScopedResult<T> result)
    {
        if (result.IsSuccess && result.Value is not null)
        {
            return Ok(ApiResponse<T>.Ok(result.Value));
        }

        if (result.NotFound)
        {
            return NotFound(ApiResponse<T>.Fail(result.Error ?? "Resource not found."));
        }

        return BadRequest(ApiResponse<T>.Fail(result.Error ?? "Request could not be processed.", result.Errors));
    }
}
