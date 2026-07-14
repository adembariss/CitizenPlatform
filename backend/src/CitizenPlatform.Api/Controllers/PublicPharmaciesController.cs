using CitizenPlatform.Api.Extensions;
using CitizenPlatform.Application.Common.Models;
using CitizenPlatform.Application.DTOs;
using CitizenPlatform.Application.Features.Pharmacies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CitizenPlatform.Api.Controllers;

[ApiController]
[Route("api/public/pharmacies")]
[EnableRateLimiting(RateLimitingPolicyNames.PublicRead)]
public sealed class PublicPharmaciesController : ControllerBase
{
    private readonly PharmacyListQueryHandler _listHandler;
    private readonly NearbyPharmacyQueryHandler _nearbyHandler;

    public PublicPharmaciesController(
        PharmacyListQueryHandler listHandler,
        NearbyPharmacyQueryHandler nearbyHandler)
    {
        _listHandler = listHandler;
        _nearbyHandler = nearbyHandler;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PharmacyDto>>>> List(
        [FromQuery] string? province,
        [FromQuery] string? district,
        [FromQuery] bool onDutyOnly = false,
        [FromQuery] int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var pharmacies = await _listHandler.HandleAsync(province, district, onDutyOnly, limit, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PharmacyDto>>.Ok(pharmacies));
    }

    [HttpGet("nearby")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PharmacyDto>>>> Nearby(
        [FromQuery(Name = "lat")] double latitude,
        [FromQuery(Name = "lng")] double longitude,
        [FromQuery] bool onDutyOnly = false,
        [FromQuery] int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var pharmacies = await _nearbyHandler.HandleAsync(latitude, longitude, onDutyOnly, limit, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PharmacyDto>>.Ok(pharmacies));
    }
}
