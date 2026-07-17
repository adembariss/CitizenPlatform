using CitizenPlatform.Api.Extensions;
using CitizenPlatform.Application.Common.Models;
using CitizenPlatform.Application.DTOs;
using CitizenPlatform.Application.Features.PublicInstitutions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CitizenPlatform.Api.Controllers;

[ApiController]
[Route("api/public/institutions")]
[EnableRateLimiting(RateLimitingPolicyNames.PublicRead)]
public sealed class PublicInstitutionsController : ControllerBase
{
    private readonly PublicInstitutionListQueryHandler _listHandler;
    private readonly PublicInstitutionCategoryListQueryHandler _categoryHandler;

    public PublicInstitutionsController(
        PublicInstitutionListQueryHandler listHandler,
        PublicInstitutionCategoryListQueryHandler categoryHandler)
    {
        _listHandler = listHandler;
        _categoryHandler = categoryHandler;
    }

    // Vatandaşın konumundaki (il/ilçe) dağıtım kurumları — elektrik/su/doğalgaz.
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PublicInstitutionDto>>>> List(
        [FromQuery] string? province,
        [FromQuery] string? district,
        [FromQuery] Guid? municipalityId,
        CancellationToken cancellationToken)
    {
        var institutions = await _listHandler.HandleAsync(province, district, municipalityId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PublicInstitutionDto>>.Ok(institutions));
    }

    [HttpGet("{institutionId:guid}/categories")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PublicCategoryDto>>>> ListCategories(
        Guid institutionId,
        CancellationToken cancellationToken)
    {
        var categories = await _categoryHandler.HandleAsync(institutionId, cancellationToken);
        if (categories is null)
        {
            return NotFound(ApiResponse<IReadOnlyList<PublicCategoryDto>>.Fail("Kurum bulunamadı."));
        }

        return Ok(ApiResponse<IReadOnlyList<PublicCategoryDto>>.Ok(categories));
    }
}
