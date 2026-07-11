using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CitizenPlatform.Application.Abstractions;
using Microsoft.IdentityModel.Tokens;

namespace CitizenPlatform.Infrastructure.Identity;

public sealed class JwtTokenService : ITokenService
{
    public const string MunicipalityIdClaimType = "municipality_id";
    public const string UserTypeClaimType = "user_type";

    private readonly JwtOptions _options;

    public JwtTokenService(JwtOptions options)
    {
        _options = options;
    }

    public AccessTokenResult CreateAccessToken(AccessTokenRequest request)
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, request.UserId.ToString()),
            new(ClaimTypes.NameIdentifier, request.UserId.ToString()),
            new(JwtRegisteredClaimNames.Email, request.Email),
            new(ClaimTypes.Name, request.DisplayName),
            new(UserTypeClaimType, request.UserType.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (request.MunicipalityId is not null)
        {
            claims.Add(new Claim(MunicipalityIdClaimType, request.MunicipalityId.Value.ToString()));
        }

        claims.AddRange(request.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        return new AccessTokenResult(accessToken, expiresAt);
    }
}
