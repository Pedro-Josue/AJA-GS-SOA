using Microsoft.AspNetCore.Identity;

namespace Aja.Api.Modules.Auth.Infrastructure;

public sealed class ApplicationUser : IdentityUser
{
    public string Alias { get; set; } = string.Empty;
}
