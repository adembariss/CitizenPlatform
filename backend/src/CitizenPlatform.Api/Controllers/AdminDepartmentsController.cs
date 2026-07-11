using CitizenPlatform.Api.Authorization;
using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common;
using CitizenPlatform.Application.Common.Models;
using CitizenPlatform.Application.DTOs;
using CitizenPlatform.Application.Features.AdminDepartments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CitizenPlatform.Api.Controllers;

[ApiController]
[Route("api/admin/departments")]
[Authorize(Policy = AuthorizationPolicyNames.RequireAdminAccess)]
public sealed class AdminDepartmentsController : ControllerBase
{
    private readonly AdminDepartmentListQueryHandler _listHandler;
    private readonly CreateDepartmentCommandHandler _createHandler;
    private readonly UpdateDepartmentCommandHandler _updateHandler;
    private readonly ICurrentUserService _currentUserService;

    public AdminDepartmentsController(
        AdminDepartmentListQueryHandler listHandler,
        CreateDepartmentCommandHandler createHandler,
        UpdateDepartmentCommandHandler updateHandler,
        ICurrentUserService currentUserService)
    {
        _listHandler = listHandler;
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DepartmentDto>>>> List(
        [FromQuery] Guid? municipalityId,
        CancellationToken cancellationToken)
    {
        var departments = await _listHandler.HandleAsync(municipalityId, TenantScope.From(_currentUserService), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<DepartmentDto>>.Ok(departments));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicyNames.RequireMunicipalityAdmin)]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> Create(
        [FromBody] CreateDepartmentCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _createHandler.HandleAsync(command, TenantScope.From(_currentUserService), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return BadRequest(ApiResponse<DepartmentDto>.Fail(result.Error ?? "Department could not be created.", result.Errors));
        }

        return StatusCode(StatusCodes.Status201Created, ApiResponse<DepartmentDto>.Ok(result.Value));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireMunicipalityAdmin)]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> Update(
        Guid id,
        [FromBody] UpdateDepartmentRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateDepartmentCommand(id, request.Name, request.IsActive);
        var result = await _updateHandler.HandleAsync(command, TenantScope.From(_currentUserService), cancellationToken);

        if (result.IsSuccess && result.Value is not null)
        {
            return Ok(ApiResponse<DepartmentDto>.Ok(result.Value));
        }

        if (result.NotFound)
        {
            return NotFound(ApiResponse<DepartmentDto>.Fail(result.Error ?? "Department not found."));
        }

        return BadRequest(ApiResponse<DepartmentDto>.Fail(result.Error ?? "Department could not be updated.", result.Errors));
    }
}
