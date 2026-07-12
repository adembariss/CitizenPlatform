using CitizenPlatform.Api.Authorization;
using CitizenPlatform.Api.Extensions;
using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common.Models;
using CitizenPlatform.Application.DTOs;
using CitizenPlatform.Application.Features.Auth;
using CitizenPlatform.Application.Features.CitizenAccounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CitizenPlatform.Api.Controllers;

[ApiController]
[Route("api/citizen/auth")]
public sealed class CitizenAuthController : ControllerBase
{
    private const string CitizenUserType = "Citizen";

    private readonly RegisterCitizenCommandHandler _registerHandler;
    private readonly LoginCommandHandler _loginHandler;
    private readonly CitizenProfileQueryHandler _profileHandler;
    private readonly ICurrentUserService _currentUser;

    public CitizenAuthController(
        RegisterCitizenCommandHandler registerHandler,
        LoginCommandHandler loginHandler,
        CitizenProfileQueryHandler profileHandler,
        ICurrentUserService currentUser)
    {
        _registerHandler = registerHandler;
        _loginHandler = loginHandler;
        _profileHandler = profileHandler;
        _currentUser = currentUser;
    }

    [HttpPost("register")]
    [EnableRateLimiting(RateLimitingPolicyNames.AuthLogin)]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Register(
        [FromBody] RegisterCitizenCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _registerHandler.HandleAsync(command, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return BadRequest(ApiResponse<LoginResponseDto>.Fail(result.Error ?? "Kayıt yapılamadı.", result.Errors));
        }

        return StatusCode(StatusCodes.Status201Created, ApiResponse<LoginResponseDto>.Ok(result.Value));
    }

    [HttpPost("login")]
    [EnableRateLimiting(RateLimitingPolicyNames.AuthLogin)]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _loginHandler.HandleAsync(command, cancellationToken);

        // Reject non-citizen accounts on the citizen surface (staff use the admin app).
        if (!result.IsSuccess || result.Value is null || !string.Equals(result.Value.User.UserType, CitizenUserType, StringComparison.Ordinal))
        {
            return Unauthorized(ApiResponse<LoginResponseDto>.Fail("E-posta veya şifre hatalı."));
        }

        return Ok(ApiResponse<LoginResponseDto>.Ok(result.Value));
    }

    [HttpGet("me")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireCitizen)]
    public async Task<ActionResult<ApiResponse<CitizenProfileDto>>> Me(CancellationToken cancellationToken)
    {
        if (_currentUser.UserGuid is not Guid userId)
        {
            return Unauthorized(ApiResponse<CitizenProfileDto>.Fail("Geçersiz oturum."));
        }

        var profile = await _profileHandler.HandleAsync(userId, cancellationToken);
        if (profile is null)
        {
            return NotFound(ApiResponse<CitizenProfileDto>.Fail("Profil bulunamadı."));
        }

        return Ok(ApiResponse<CitizenProfileDto>.Ok(profile));
    }
}
