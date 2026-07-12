using CitizenPlatform.Api.Extensions;
using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common.Models;
using CitizenPlatform.Application.DTOs;
using CitizenPlatform.Application.Features.PublicCategories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CitizenPlatform.Api.Controllers;

[ApiController]
[Route("api/public/municipalities")]
[EnableRateLimiting(RateLimitingPolicyNames.PublicWrite)]
public sealed class PublicMunicipalitiesController : ControllerBase
{
    private readonly IGeoMunicipalityResolver _geoMunicipalityResolver;
    private readonly PublicCategoryListQueryHandler _categoryListHandler;

    public PublicMunicipalitiesController(
        IGeoMunicipalityResolver geoMunicipalityResolver,
        PublicCategoryListQueryHandler categoryListHandler)
    {
        _geoMunicipalityResolver = geoMunicipalityResolver;
        _categoryListHandler = categoryListHandler;
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

    [HttpGet("{municipalityId:guid}/categories")]
    [EnableRateLimiting(RateLimitingPolicyNames.PublicRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PublicCategoryDto>>>> ListCategories(
        Guid municipalityId,
        CancellationToken ct)
    {
        var categories = await _categoryListHandler.HandleAsync(municipalityId, ct);
        if (categories is null)
        {
            return NotFound(ApiResponse<IReadOnlyList<PublicCategoryDto>>.Fail("Municipality could not be found."));
        }

        return Ok(ApiResponse<IReadOnlyList<PublicCategoryDto>>.Ok(categories));
    }
}
