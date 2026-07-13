using CitizenPlatform.Api.Extensions;
using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common.Models;
using CitizenPlatform.Application.DTOs;
using CitizenPlatform.Application.Features.PublicCategories;
using CitizenPlatform.Application.Features.PublicDirectory;
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
    private readonly ProvinceListQueryHandler _provinceListHandler;
    private readonly DistrictListQueryHandler _districtListHandler;

    public PublicMunicipalitiesController(
        IGeoMunicipalityResolver geoMunicipalityResolver,
        PublicCategoryListQueryHandler categoryListHandler,
        ProvinceListQueryHandler provinceListHandler,
        DistrictListQueryHandler districtListHandler)
    {
        _geoMunicipalityResolver = geoMunicipalityResolver;
        _categoryListHandler = categoryListHandler;
        _provinceListHandler = provinceListHandler;
        _districtListHandler = districtListHandler;
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

    [HttpGet("/api/public/provinces")]
    [EnableRateLimiting(RateLimitingPolicyNames.PublicRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<string>>>> ListProvinces(CancellationToken ct)
    {
        var provinces = await _provinceListHandler.HandleAsync(ct);
        return Ok(ApiResponse<IReadOnlyList<string>>.Ok(provinces));
    }

    [HttpGet("/api/public/districts")]
    [EnableRateLimiting(RateLimitingPolicyNames.PublicRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PublicDistrictDto>>>> ListDistricts(
        [FromQuery] string province,
        CancellationToken ct)
    {
        var districts = await _districtListHandler.HandleAsync(province, ct);
        return Ok(ApiResponse<IReadOnlyList<PublicDistrictDto>>.Ok(districts));
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
