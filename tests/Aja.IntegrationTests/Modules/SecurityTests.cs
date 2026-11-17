using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace Aja.IntegrationTests.Modules;

public sealed class SecurityTests
{
    [Fact]
    public async Task CadastroJwtRoleEAutoria_SaoAplicadosPeloPipelineReal()
    {
        using var factory = new AjaFactory();
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/demandas")).StatusCode);

        var emailA = $"a-{Guid.NewGuid():N}@aja.test";
        var registration = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = emailA,
            alias = "Pessoa A",
            password = "User#Aja2026!",
            role = "Admin",
            isAdmin = true
        });
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
        var registrationBody = await registration.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", registrationBody.ToLowerInvariant());
        Assert.DoesNotContain("hash", registrationBody.ToLowerInvariant());
        var profile = JsonDocument.Parse(registrationBody).RootElement;
        Assert.Equal("User", profile.GetProperty("role").GetString());

        var tokenA = await client.LoginAsync(emailA, "User#Aja2026!");
        Assert.Equal(3, tokenA.Split('.').Length);
        client.Bearer(tokenA);
        var demandId = await client.CreateDemandAsync();

        var invalidDemand = await client.PostAsJsonAsync("/api/v1/demandas", new
        {
            descricao = "curta",
            local = "x",
            categoria = "Fisica",
            autorId = Guid.NewGuid()
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalidDemand.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.PatchAsJsonAsync($"/api/v1/demandas/{demandId}", new { categoria = "Fisica" })).StatusCode);

        var (_, tokenB) = await client.RegisterAndLoginAsync("Pessoa B");
        client.Bearer(tokenB);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync($"/api/v1/demandas/{demandId}", new
        {
            descricao = "Tentativa de alteração por outra pessoa.",
            local = "Outro local público"
        })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync($"/api/v1/demandas/{demandId}")).StatusCode);

        var adminToken = await client.LoginAsync("admin@aja.test", "Admin#Aja2026!");
        client.Bearer(adminToken);
        var adminUpdate = await client.PatchAsJsonAsync($"/api/v1/demandas/{demandId}", new { categoria = "Fisica" });
        Assert.Equal(HttpStatusCode.OK, adminUpdate.StatusCode);
    }

    [Fact]
    public async Task CadastroELogin_AceitamSenhaNumericaDeQuatroCaracteres()
    {
        using var factory = new AjaFactory();
        using var client = factory.CreateClient();
        var email = $"senha-simples-{Guid.NewGuid():N}@aja.test";
        var registration = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            alias = "Senha simples",
            password = "1234"
        });
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
        var token = await client.LoginAsync(email, "1234");
        Assert.Equal(3, token.Split('.').Length);
    }

    [Fact]
    public async Task BloqueioAposCincoFalhas_ImpedeLoginMesmoComSenhaCorreta()
    {
        using var factory = new AjaFactory();
        using var client = factory.CreateClient();
        var (email, _) = await client.RegisterAndLoginAsync("Conta de bloqueio");
        client.DefaultRequestHeaders.Authorization = null;

        for (var i = 0; i < 5; i++)
        {
            var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Senha#Errada2026!" });
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        var locked = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "User#Aja2026!" });
        Assert.Equal(HttpStatusCode.Unauthorized, locked.StatusCode);
    }

    [Fact]
    public async Task TokensComAssinaturaEmissorAudienciaOuExpiracaoInvalidos_Retornam401()
    {
        using var factory = new AjaFactory();
        using var client = factory.CreateClient();
        var tokens = new[]
        {
            CreateToken("outra-chave-de-assinatura-com-mais-de-trinta-e-dois-bytes", "Aja.IntegrationTests", "Aja.TestClients", DateTime.UtcNow.AddMinutes(10)),
            CreateToken(AjaFactory.JwtKey, "emissor-errado", "Aja.TestClients", DateTime.UtcNow.AddMinutes(10)),
            CreateToken(AjaFactory.JwtKey, "Aja.IntegrationTests", "audiencia-errada", DateTime.UtcNow.AddMinutes(10)),
            CreateToken(AjaFactory.JwtKey, "Aja.IntegrationTests", "Aja.TestClients", DateTime.UtcNow.AddMinutes(-2))
        };

        foreach (var token in tokens)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(request)).StatusCode);
        }

        using var tampered = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        tampered.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens[0][..^2] + "aa");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(tampered)).StatusCode);
    }

    [Fact]
    public async Task BootstrapAdmin_EIdempotenteEMaisSeguroQuePromoverContaExistente()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aja-admin-collision-{Guid.NewGuid():N}.db");
        const string collisionEmail = "collision@aja.test";
        try
        {
            using (var initial = new AjaFactory(path, false, enableAdminBootstrap: false))
            using (var client = initial.CreateClient())
            {
                var created = await client.PostAsJsonAsync("/api/v1/auth/register", new
                {
                    email = collisionEmail,
                    alias = "Conta existente",
                    password = "User#Aja2026!"
                });
                Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            }

            using (var collision = new AjaFactory(path, false, enableAdminBootstrap: true, adminEmail: collisionEmail))
                Assert.Throws<InvalidOperationException>(() => collision.CreateClient());

            using (var verification = new AjaFactory(path, false, enableAdminBootstrap: false))
            using (var client = verification.CreateClient())
            {
                var token = await client.LoginAsync(collisionEmail, "User#Aja2026!");
                client.Bearer(token);
                var result = await client.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");
                Assert.Equal("User", result.GetProperty("role").GetString());
            }
        }
        finally
        {
            AjaFactory.DeleteDatabaseFiles(path);
        }
    }

    private static string CreateToken(string key, string issuer, string audience, DateTime expires)
    {
        var now = DateTime.UtcNow.AddHours(-1);
        var token = new JwtSecurityToken(issuer, audience,
            [new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()), new Claim(ClaimTypes.Role, "User")],
            now, expires, new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
