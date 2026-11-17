using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Aja.Api.Modules.Auth.Application;
using Aja.Api.Modules.Auth.Domain;
using Aja.Api.Shared.Errors;

namespace Aja.Api.Modules.Auth.Infrastructure;

public sealed class HttpCurrentUser(IHttpContextAccessor contextAccessor) : ICurrentUser
{
    private ClaimsPrincipal Principal => contextAccessor.HttpContext?.User
        ?? throw AjaProblemException.Unauthorized();

    public string Id => Principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? Principal.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw AjaProblemException.Unauthorized();

    public bool IsAdmin => Principal.IsInRole(Perfis.Admin);
}
