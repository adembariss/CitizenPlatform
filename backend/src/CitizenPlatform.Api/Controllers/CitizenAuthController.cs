using CitizenPlatform.Api.Authorization;
using CitizenPlatform.Api.Extensions;
using CitizenPlatform.Api.Models;
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
    private readonly VerifyPhoneCommandHandler _verifyPhoneHandler;
    private readonly ResendCodeCommandHandler _resendCodeHandler;
    private readonly ICurrentUserService _currentUser;

    public CitizenAuthController(
        RegisterCitizenCommandHandler registerHandler,
        LoginCommandHandler loginHandler,
        CitizenProfileQueryHandler profileHandler,
        VerifyPhoneCommandHandler verifyPhoneHandler,
        ResendCodeCommandHandler resendCodeHandler,
        ICurrentUserService currentUser)
    {
        _registerHandler = registerHandler;
        _loginHandler = loginHandler;
        _profileHandler = profileHandler;
        _verifyPhoneHandler = verifyPhoneHandler;
        _resendCodeHandler = resendCodeHandler;
        _currentUser = currentUser;
    }

    [HttpPost("register")]
    [EnableRateLimiting(RateLimitingPolicyNames.AuthLogin)]
    public async Task<ActionResult<ApiResponse<CitizenAuthResultDto>>> Register(
        [FromBody] RegisterCitizenCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _registerHandler.HandleAsync(command, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return BadRequest(ApiResponse<CitizenAuthResultDto>.Fail(result.Error ?? "Kayıt yapılamadı.", result.Errors));
        }

        return StatusCode(StatusCodes.Status201Created, ApiResponse<CitizenAuthResultDto>.Ok(result.Value));
    }

    [HttpPost("verify-phone")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireCitizen)]
    public async Task<ActionResult<ApiResponse<object?>>> VerifyPhone(
        [FromBody] VerifyPhoneRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserGuid is not Guid userId)
        {
            return Unauthorized(ApiResponse<object?>.Fail("Geçersiz oturum."));
        }

        var result = await _verifyPhoneHandler.HandleAsync(new VerifyPhoneCommand(userId, request.Code ?? string.Empty), cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object?>.Fail(result.Error ?? "Doğrulama başarısız."));
        }

        return Ok(ApiResponse<object?>.Ok(null, "Telefon doğrulandı."));
    }

    [HttpPost("resend-code")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireCitizen)]
    [EnableRateLimiting(RateLimitingPolicyNames.AuthLogin)]
    public async Task<ActionResult<ApiResponse<ResendCodeResult>>> ResendCode(CancellationToken cancellationToken)
    {
        if (_currentUser.UserGuid is not Guid userId)
        {
            return Unauthorized(ApiResponse<ResendCodeResult>.Fail("Geçersiz oturum."));
        }

        var result = await _resendCodeHandler.HandleAsync(new ResendCodeCommand(userId), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return BadRequest(ApiResponse<ResendCodeResult>.Fail(result.Error ?? "Kod gönderilemedi."));
        }

        return Ok(ApiResponse<ResendCodeResult>.Ok(result.Value));
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
