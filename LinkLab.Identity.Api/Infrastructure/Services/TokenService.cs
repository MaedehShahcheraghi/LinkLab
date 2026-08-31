using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LinkLab.Identity.Api.Core.Constants;
using LinkLab.Identity.Api.Core.Interfaces;
using LinkLab.Identity.Api.Core.Options;
using LinkLab.Identity.Api.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LinkLab.Identity.Api.Infrastructure.Services;

public sealed class TokenService(
    IOptions<JwtOptions> options,
    ITokenContext tokenContext,
    TimeProvider timeProvider) : ITokenService
{
    private readonly JwtOptions _options = options.Value;

    public string GenerateAccessToken(ApplicationUser user, IList<string> roles, long permissionMask)
    {
        var userAgent = tokenContext.UserAgent ?? string.Empty;

        var now = timeProvider.GetUtcNow();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat,   now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new(CustomClaims.Permission,       permissionMask.ToString()),
            new(CustomClaims.AuthzVersion,     user.AuthzVersion.ToString()),
        };

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer:            _options.Issuer,
            audience:          _options.Audience,
            claims:            claims,
            notBefore:         now.UtcDateTime,
            expires:           now.AddMinutes(_options.ExpiryMinutes).UtcDateTime,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
