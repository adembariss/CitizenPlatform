using CitizenPlatform.Api.Extensions;
using CitizenPlatform.Application.Common.Models;
using CitizenPlatform.Application.DTOs;
using CitizenPlatform.Application.Features.PublicInsights;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CitizenPlatform.Api.Controllers;

[ApiController]
[Route("api/public")]
[EnableRateLimiting(RateLimitingPolicyNames.PublicRead)]
public sealed class PublicInsightsController : ControllerBase
{
    private readonly PublicStatsQueryHandler _statsHandler;
    private readonly PublicComplaintMapQueryHandler _mapHandler;

    public PublicInsightsController(
        PublicStatsQueryHandler statsHandler,
        PublicComplaintMapQueryHandler mapHandler)
    {
        _statsHandler = statsHandler;
        _mapHandler = mapHandler;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<PublicStatsDto>>> GetStats(CancellationToken cancellationToken)
    {
        var stats = await _statsHandler.HandleAsync(cancellationToken);
        return Ok(ApiResponse<PublicStatsDto>.Ok(stats));
    }

    [HttpGet("complaints/map")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PublicComplaintMapPointDto>>>> GetMapPoints(
        [FromQuery] Guid? municipalityId,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        var points = await _mapHandler.HandleAsync(municipalityId, limit, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PublicComplaintMapPointDto>>.Ok(points));
    }
}
