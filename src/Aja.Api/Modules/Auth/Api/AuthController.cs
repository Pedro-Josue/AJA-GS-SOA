using Aja.Api.Modules.Auth.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aja.Api.Modules.Auth.Api;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IAuthService service) : ControllerBase
{
    /// <summary>Cadastra uma conta sempre com o perfil User.</summary>
    /// <remarks>Exemplo acadêmico: { "email": "pessoa@example.invalid", "alias": "Pessoa", "password": "1234" }</remarks>
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType<ProfileResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProfileResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var profile = await service.RegisterAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, profile);
    }

    /// <summary>Valida as credenciais e emite um JWT Bearer.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken) =>
        Ok(await service.LoginAsync(request, cancellationToken));

    [HttpGet("me")]
    [ProducesResponseType<ProfileResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProfileResponse>> Me(CancellationToken cancellationToken) =>
        Ok(await service.GetMeAsync(cancellationToken));

}
