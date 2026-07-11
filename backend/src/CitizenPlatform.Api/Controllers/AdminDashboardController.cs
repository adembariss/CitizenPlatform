using CitizenPlatform.Api.Authorization;
using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common;
using CitizenPlatform.Application.Common.Models;
using CitizenPlatform.Application.DTOs;
using CitizenPlatform.Application.Features.AdminDashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CitizenPlatform.Api.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Policy = AuthorizationPolicyNames.RequireAdminAccess)]
public sealed class AdminDashboardController : ControllerBase
{
    private readonly DashboardSummaryQueryHandler _summaryHandler;
    private readonly ICurrentUserService _currentUserService;

    public AdminDashboardController(DashboardSummaryQueryHandler summaryHandler, ICurrentUserService currentUserService)
    {
        _summaryHandler = summaryHandler;
        _currentUserService = currentUserService;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<DashboardSummaryDto>>> Summary(
        [FromQuery] Guid? municipalityId,
        CancellationToken cancellationToken)
    {
        var summary = await _summaryHandler.HandleAsync(municipalityId, TenantScope.From(_currentUserService), cancellationToken);
        return Ok(ApiResponse<DashboardSummaryDto>.Ok(summary));
    }
}
