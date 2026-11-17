using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace Aja.IntegrationTests;

public sealed class AjaFactory(
    string? databasePath = null,
    bool deleteDatabaseOnDispose = true,
    bool enableAdminBootstrap = true,
    string adminEmail = "admin@aja.test")
    : WebApplicationFactory<Program>
{
    public const string JwtKey = "AJA-integration-tests-signing-key-with-more-than-32-bytes-2026";
    public string DatabasePath { get; } = databasePath ?? Path.Combine(Path.GetTempPath(), $"aja-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Aja"] = $"Data Source={DatabasePath};Pooling=False",
            ["Jwt:Issuer"] = "Aja.IntegrationTests",
            ["Jwt:Audience"] = "Aja.TestClients",
            ["Jwt:SigningKey"] = JwtKey,
            ["Jwt:ExpirationMinutes"] = "30",
            ["AdminBootstrap:Enabled"] = enableAdminBootstrap.ToString(),
            ["AdminBootstrap:Email"] = adminEmail,
            ["AdminBootstrap:Password"] = "Admin#Aja2026!",
            ["AdminBootstrap:Alias"] = "Admin Teste"
        }));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
        if (deleteDatabaseOnDispose)
            DeleteDatabaseFiles(DatabasePath);
    }

    public static void DeleteDatabaseFiles(string path)
    {
        foreach (var file in new[] { path, path + "-shm", path + "-wal" })
            if (File.Exists(file)) File.Delete(file);
    }
}
