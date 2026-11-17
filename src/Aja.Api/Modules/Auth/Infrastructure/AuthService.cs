using Aja.Api.Modules.Auth.Application;
using Aja.Api.Modules.Auth.Domain;
using Aja.Api.Shared.Errors;
using Microsoft.AspNetCore.Identity;

namespace Aja.Api.Modules.Auth.Infrastructure;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IJwtTokenService tokenService,
    ICurrentUser currentUser) : IAuthService
{
    public async Task<ProfileResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        if (await userManager.FindByEmailAsync(email) is not null)
            throw AjaProblemException.Conflict("Já existe uma conta com esse email.", "email_duplicado");

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = email,
            Email = email,
            Alias = request.Alias.Trim()
        };
        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw AjaProblemException.BadRequest(string.Join(" ", result.Errors.Select(e => e.Description)), "cadastro_invalido");

        var roleResult = await userManager.AddToRoleAsync(user, Perfis.User);
        if (!roleResult.Succeeded)
            throw new InvalidOperationException("Não foi possível atribuir o perfil User.");
        return ToProfile(user, Perfis.User);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null) throw AjaProblemException.Unauthorized();
        var check = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!check.Succeeded) throw AjaProblemException.Unauthorized();

        var roles = await userManager.GetRolesAsync(user);
        var role = roles.SingleOrDefault() ?? Perfis.User;
        var generated = tokenService.Create(user.Id, user.Email!, user.Alias, role);
        return new LoginResponse(generated.Token, "Bearer", generated.ExpiresIn, ToProfile(user, role));
    }

    public async Task<ProfileResponse> GetMeAsync(CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(currentUser.Id) ?? throw AjaProblemException.NotFound();
        var role = (await userManager.GetRolesAsync(user)).Single();
        return ToProfile(user, role);
    }

    private static ProfileResponse ToProfile(ApplicationUser user, string role) =>
        new(user.Id, user.Email!, user.Alias, role);
}
