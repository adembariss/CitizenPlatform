using CitizenPlatform.Api.Extensions;
using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common.Models;
using CitizenPlatform.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CitizenPlatform.Api.Controllers;

[ApiController]
[Route("api/public/municipalities")]
[EnableRateLimiting(RateLimitingPolicyNames.PublicWrite)]
public sealed class PublicMunicipalitiesController : ControllerBase
{
    private readonly IGeoMunicipalityResolver _geoMunicipalityResolver;

    public PublicMunicipalitiesController(IGeoMunicipalityResolver geoMunicipalityResolver)
    {
        _geoMunicipalityResolver = geoMunicipalityResolver;
    }

    [HttpGet("resolve")]
    public async Task<ActionResult<ApiResponse<MunicipalityResolveResult>>> Resolve(
        [FromQuery(Name = "lat")] double latitude,
        [FromQuery(Name = "lng")] double longitude,
        CancellationToken ct)
    {
        var result = await _geoMunicipalityResolver.ResolveByCoordinateAsync(latitude, longitude, ct);

        if (result.IsSuccess)
        {
            return Ok(ApiResponse<MunicipalityResolveResult>.Ok(result));
        }

        return BadRequest(ApiResponse<MunicipalityResolveResult>.Fail(
            result,
            result.FailureReason ?? "Municipality could not be resolved."));
    }
}
