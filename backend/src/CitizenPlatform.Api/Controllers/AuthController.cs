using System.Security.Claims;
using CitizenPlatform.Api.Extensions;
using CitizenPlatform.Application.Common.Models;
using CitizenPlatform.Application.DTOs;
using CitizenPlatform.Application.Features.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CitizenPlatform.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly LoginCommandHandler _loginHandler;
    private readonly GetCurrentUserQueryHandler _getCurrentUserHandler;
    private readonly RefreshTokenCommandHandler _refreshTokenHandler;
    private readonly LogoutCommandHandler _logoutHandler;

    public AuthController(
        LoginCommandHandler loginHandler,
        GetCurrentUserQueryHandler getCurrentUserHandler,
        RefreshTokenCommandHandler refreshTokenHandler,
        LogoutCommandHandler logoutHandler)
    {
        _loginHandler = loginHandler;
        _getCurrentUserHandler = getCurrentUserHandler;
        _refreshTokenHandler = refreshTokenHandler;
        _logoutHandler = logoutHandler;
    }

    [HttpPost("login")]
    [EnableRateLimiting(RateLimitingPolicyNames.AuthLogin)]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _loginHandler.HandleAsync(command, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return Unauthorized(ApiResponse<LoginResponseDto>.Fail(result.Error ?? "Invalid email or password.", result.Errors));
        }

        return Ok(ApiResponse<LoginResponseDto>.Ok(result.Value));
    }

    [HttpPost("refresh")]
    [EnableRateLimiting(RateLimitingPolicyNames.PublicRead)]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Refresh(
        [FromBody] RefreshTokenCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _refreshTokenHandler.HandleAsync(command, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return Unauthorized(ApiResponse<LoginResponseDto>.Fail(result.Error ?? "Invalid or expired refresh token.", result.Errors));
        }

        return Ok(ApiResponse<LoginResponseDto>.Ok(result.Value));
    }

    [HttpPost("logout")]
    [EnableRateLimiting(RateLimitingPolicyNames.PublicRead)]
    public async Task<ActionResult<ApiResponse<object?>>> Logout(
        [FromBody] RefreshTokenCommand command,
        CancellationToken cancellationToken)
    {
        await _logoutHandler.HandleAsync(command, cancellationToken);
        return Ok(ApiResponse<object?>.Ok(null));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<CurrentUserDto>>> Me(CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized(ApiResponse<CurrentUserDto>.Fail("Invalid token."));
        }

        var result = await _getCurrentUserHandler.HandleAsync(userId, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return Unauthorized(ApiResponse<CurrentUserDto>.Fail(result.Error ?? "User not found."));
        }

        return Ok(ApiResponse<CurrentUserDto>.Ok(result.Value));
    }
}
