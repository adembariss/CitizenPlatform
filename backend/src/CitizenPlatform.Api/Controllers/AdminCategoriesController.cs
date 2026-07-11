using CitizenPlatform.Api.Authorization;
using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common;
using CitizenPlatform.Application.Common.Models;
using CitizenPlatform.Application.DTOs;
using CitizenPlatform.Application.Features.AdminCategories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CitizenPlatform.Api.Controllers;

[ApiController]
[Route("api/admin/categories")]
[Authorize(Policy = AuthorizationPolicyNames.RequireAdminAccess)]
public sealed class AdminCategoriesController : ControllerBase
{
    private readonly AdminCategoryListQueryHandler _listHandler;
    private readonly CreateCategoryCommandHandler _createHandler;
    private readonly UpdateCategoryCommandHandler _updateHandler;
    private readonly ICurrentUserService _currentUserService;

    public AdminCategoriesController(
        AdminCategoryListQueryHandler listHandler,
        CreateCategoryCommandHandler createHandler,
        UpdateCategoryCommandHandler updateHandler,
        ICurrentUserService currentUserService)
    {
        _listHandler = listHandler;
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CategoryDto>>>> List(
        [FromQuery] Guid? municipalityId,
        CancellationToken cancellationToken)
    {
        var categories = await _listHandler.HandleAsync(municipalityId, TenantScope.From(_currentUserService), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CategoryDto>>.Ok(categories));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicyNames.RequireMunicipalityAdmin)]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> Create(
        [FromBody] CreateCategoryCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _createHandler.HandleAsync(command, TenantScope.From(_currentUserService), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return BadRequest(ApiResponse<CategoryDto>.Fail(result.Error ?? "Category could not be created.", result.Errors));
        }

        return StatusCode(StatusCodes.Status201Created, ApiResponse<CategoryDto>.Ok(result.Value));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireMunicipalityAdmin)]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> Update(
        Guid id,
        [FromBody] UpdateCategoryRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateCategoryCommand(id, request.Name, request.IsActive);
        var result = await _updateHandler.HandleAsync(command, TenantScope.From(_currentUserService), cancellationToken);

        if (result.IsSuccess && result.Value is not null)
        {
            return Ok(ApiResponse<CategoryDto>.Ok(result.Value));
        }

        if (result.NotFound)
        {
            return NotFound(ApiResponse<CategoryDto>.Fail(result.Error ?? "Category not found."));
        }

        return BadRequest(ApiResponse<CategoryDto>.Fail(result.Error ?? "Category could not be updated.", result.Errors));
    }
}
