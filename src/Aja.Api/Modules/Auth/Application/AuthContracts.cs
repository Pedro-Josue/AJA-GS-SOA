using System.ComponentModel.DataAnnotations;

namespace Aja.Api.Modules.Auth.Application;

public sealed record RegisterRequest(
    [param: Required, EmailAddress] string Email,
    [param: Required, StringLength(60, MinimumLength = 3)] string Alias,
    [param: Required, MinLength(4)] string Password);

public sealed record LoginRequest(
    [param: Required, EmailAddress] string Email,
    [param: Required] string Password);

public sealed record ProfileResponse(string Id, string Email, string Alias, string Role);
public sealed record LoginResponse(string AccessToken, string TokenType, int ExpiresIn, ProfileResponse Profile);

public interface ICurrentUser
{
    string Id { get; }
    bool IsAdmin { get; }
}

public interface IJwtTokenService
{
    (string Token, int ExpiresIn) Create(string userId, string email, string alias, string role);
}

public interface IAuthService
{
    Task<ProfileResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<ProfileResponse> GetMeAsync(CancellationToken cancellationToken);
}
