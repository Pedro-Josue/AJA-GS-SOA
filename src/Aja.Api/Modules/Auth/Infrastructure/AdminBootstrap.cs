using Aja.Api.Modules.Auth.Domain;
using Microsoft.AspNetCore.Identity;

namespace Aja.Api.Modules.Auth.Infrastructure;

public static class AdminBootstrap
{
    public static async Task ExecuteAsync(IServiceProvider services, IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in Perfis.Todos)
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));

        if (!configuration.GetValue<bool>("AdminBootstrap:Enabled")) return;

        var email = configuration["AdminBootstrap:Email"]?.Trim();
        var password = configuration["AdminBootstrap:Password"];
        var alias = configuration["AdminBootstrap:Alias"]?.Trim() ?? "Admin AJA";
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("AdminBootstrap habilitado exige Email e Password na configuração local.");

        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var existing = await users.FindByEmailAsync(email);
        if (existing is not null)
        {
            if (!await users.IsInRoleAsync(existing, Perfis.Admin))
                throw new InvalidOperationException("O email do bootstrap já pertence a uma conta User. Use outro email; nenhuma promoção foi realizada.");
            return;
        }

        var admin = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = email,
            Email = email,
            Alias = alias
        };
        var created = await users.CreateAsync(admin, password);
        if (!created.Succeeded)
            throw new InvalidOperationException("Falha no bootstrap Admin: " + string.Join(" ", created.Errors.Select(e => e.Description)));
        var added = await users.AddToRoleAsync(admin, Perfis.Admin);
        if (!added.Succeeded)
            throw new InvalidOperationException("Falha ao associar a role Admin.");
    }
}
