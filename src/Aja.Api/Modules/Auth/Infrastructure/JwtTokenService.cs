using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Aja.Api.Modules.Auth.Application;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Aja.Api.Modules.Auth.Infrastructure;

public sealed class JwtTokenService(IOptions<JwtOptions> options, TimeProvider clock) : IJwtTokenService
{
    public (string Token, int ExpiresIn) Create(string userId, string email, string alias, string role)
    {
        var settings = options.Value;
        var now = clock.GetUtcNow();
        var expires = now.AddMinutes(settings.ExpirationMinutes);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)),
            SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new Claim(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim("alias", alias),
            new Claim(ClaimTypes.Role, role)
        };
        var token = new JwtSecurityToken(
            settings.Issuer,
            settings.Audience,
            claims,
            now.UtcDateTime,
            expires.UtcDateTime,
            credentials);
        return (new JwtSecurityTokenHandler().WriteToken(token), settings.ExpirationMinutes * 60);
    }
}
