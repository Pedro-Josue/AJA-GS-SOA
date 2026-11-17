using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Aja.IntegrationTests;

internal static class ApiClientExtensions
{
    private static int _counter;

    public static async Task<(string Email, string Token)> RegisterAndLoginAsync(this HttpClient client, string alias)
    {
        var suffix = Interlocked.Increment(ref _counter);
        var email = $"user{suffix}-{Guid.NewGuid():N}@aja.test";
        var register = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            alias,
            password = "User#Aja2026!",
            role = "Admin"
        });
        register.EnsureSuccessStatusCode();
        var token = await client.LoginAsync(email, "User#Aja2026!");
        return (email, token);
    }

    public static async Task<string> LoginAsync(this HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("accessToken").GetString()!;
    }

    public static void Bearer(this HttpClient client, string token) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    public static async Task<Guid> CreateDemandAsync(this HttpClient client, string? description = null, string? location = null)
    {
        var response = await client.PostAsJsonAsync("/api/v1/demandas", new
        {
            descricao = description ?? "Exemplo fictício de obstrução da faixa livre de circulação em uma passagem pública.",
            local = location ?? "Passagem pública demonstrativa, sem coordenadas reais"
        });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("demanda").GetProperty("id").GetGuid();
    }
}
