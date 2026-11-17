using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Aja.Api.Modules.Auth.Infrastructure;
using Aja.Api.Modules.Demandas.Domain;
using Aja.Api.Modules.Participacoes.Domain;
using Aja.Api.Shared.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.DependencyInjection;

namespace Aja.IntegrationTests.Modules;

public sealed class PersistenceAndDocumentationTests
{
    [Fact]
    public async Task OpenApi_ExpoeAsQuinzeOperacoesSimplificadasEJwtBearer()
    {
        using var factory = new AjaFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var document = await response.Content.ReadFromJsonAsync<JsonElement>();
        var paths = document.GetProperty("paths");

        Assert.True(paths.TryGetProperty("/api/v1/auth/register", out _));
        Assert.True(paths.TryGetProperty("/api/v1/auth/login", out _));
        Assert.True(paths.TryGetProperty("/api/v1/auth/me", out _));
        Assert.True(paths.TryGetProperty("/api/v1/demandas", out _));
        Assert.True(paths.TryGetProperty("/api/v1/demandas/{id}", out _));
        Assert.True(paths.TryGetProperty("/api/v1/demandas/{demandaId}/apoio", out _));
        Assert.True(paths.TryGetProperty("/api/v1/demandas/{demandaId}/comentarios", out _));
        Assert.True(paths.TryGetProperty("/api/v1/comentarios/{id}", out _));
        Assert.True(paths.TryGetProperty("/api/v1/comentarios/{id}/moderacao", out _));
        Assert.True(paths.TryGetProperty("/health", out _));
        Assert.False(paths.TryGetProperty("/api/v1/relatos", out _));
        Assert.False(paths.TryGetProperty("/api/v1/topicos", out _));

        var functionalOperations = paths.EnumerateObject()
            .Where(x => x.Name.StartsWith("/api/v1/", StringComparison.Ordinal) && x.Name != "/api/v1/health")
            .Sum(x => x.Value.EnumerateObject().Count(p => p.Name is "get" or "post" or "put" or "patch" or "delete"));
        Assert.Equal(15, functionalOperations);
        var loginOperation = paths.GetProperty("/api/v1/auth/login").GetProperty("post");
        Assert.False(loginOperation.TryGetProperty("security", out var loginSecurity) && loginSecurity.GetArrayLength() > 0);
        var bearer = document.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        Assert.Equal("http", bearer.GetProperty("type").GetString());
        Assert.Equal("bearer", bearer.GetProperty("scheme").GetString());
    }

    [Fact]
    public async Task MigracoesChavesEstrangeirasEUnicidadeDeApoio_SaoReais()
    {
        using var factory = new AjaFactory();
        using var client = factory.CreateClient();
        var (_, token) = await client.RegisterAndLoginAsync("Persistência relacional");
        client.Bearer(token);
        var demandId = await client.CreateDemandAsync();
        await client.PutAsync($"/api/v1/demandas/{demandId}/apoio", null);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AjaDbContext>();
        var applied = await db.Database.GetAppliedMigrationsAsync();
        Assert.Contains(applied, x => x.EndsWith("InitialCreate"));
        Assert.Contains(applied, x => x.EndsWith("SimplifiedDemandFlow"));
        var user = await db.Users.SingleAsync(x => x.NormalizedEmail!.StartsWith("USER"));
        Assert.False(string.IsNullOrWhiteSpace(user.PasswordHash));

        db.Apoios.Add(new Apoio(demandId, user.Id, DateTimeOffset.UtcNow));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

        db.ChangeTracker.Clear();
        db.Comentarios.Add(new Comentario(Guid.NewGuid(), Guid.NewGuid().ToString(), "Referências inexistentes.", DateTimeOffset.UtcNow));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task DadosPermanecemDepoisDeReiniciarAplicacaoComMesmoArquivo()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aja-persistence-{Guid.NewGuid():N}.db");
        string email;
        try
        {
            using (var first = new AjaFactory(path, deleteDatabaseOnDispose: false))
            using (var client = first.CreateClient())
            {
                var registered = await client.RegisterAndLoginAsync("Persistência Teste");
                email = registered.Email;
                client.Bearer(registered.Token);
                _ = await client.CreateDemandAsync();
            }

            using (var second = new AjaFactory(path, deleteDatabaseOnDispose: false))
            using (var client = second.CreateClient())
            {
                var token = await client.LoginAsync(email, "User#Aja2026!");
                client.Bearer(token);
                var demands = await client.GetFromJsonAsync<JsonElement>("/api/v1/demandas?minhas=true");
                Assert.Equal(1, demands.GetProperty("totalItems").GetInt32());
            }
        }
        finally
        {
            AjaFactory.DeleteDatabaseFiles(path);
        }
    }

    [Fact]
    public async Task MigracaoDeBaseAnteriorPreenchida_PreservaIdentidadeAutoriaVisibilidadeEVinculos()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aja-legacy-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<AjaDbContext>().UseSqlite($"Data Source={path};Pooling=False").Options;
        const string adminId = "legacy-admin";
        const string userId = "legacy-user";
        const string userEmail = "legacy-user@aja.test";
        var topicId = Guid.NewGuid();
        var isolatedReportId = Guid.NewGuid();
        var linkedReportA = Guid.NewGuid();
        var linkedReportB = Guid.NewGuid();
        var supportId = Guid.NewGuid();
        var visibleParticipationId = Guid.NewGuid();
        var hiddenParticipationId = Guid.NewGuid();
        var dossierId = Guid.NewGuid();
        var compatibleTrackingId = Guid.NewGuid();
        var preservedTrackingId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 11, 14, 12, 0, 0, TimeSpan.Zero);
        var storedNow = Store(now);
        string userHash;

        try
        {
            await using (var db = new AjaDbContext(options))
            {
                var migrator = db.Database.GetService<IMigrator>();
                await migrator.MigrateAsync("20261114051059_InitialCreate");
                var admin = NewUser(adminId, "legacy-admin@aja.test", "Admin legado");
                var user = NewUser(userId, userEmail, "Pessoa legada");
                var hasher = new PasswordHasher<ApplicationUser>();
                admin.PasswordHash = hasher.HashPassword(admin, "Admin#Legacy2026!");
                user.PasswordHash = hasher.HashPassword(user, "User#Legacy2026!");
                userHash = user.PasswordHash;
                var adminRole = new IdentityRole("Admin") { Id = "role-admin", NormalizedName = "ADMIN" };
                var userRole = new IdentityRole("User") { Id = "role-user", NormalizedName = "USER" };
                db.Users.AddRange(admin, user);
                db.Roles.AddRange(adminRole, userRole);
                db.UserRoles.AddRange(
                    new IdentityUserRole<string> { UserId = adminId, RoleId = adminRole.Id },
                    new IdentityUserRole<string> { UserId = userId, RoleId = userRole.Id });
                await db.SaveChangesAsync();

                await db.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO Topicos
                        (Id,Titulo,Contexto,Categoria,Cidade,Bairro,ReferenciaLocal,Situacao,Demonstrativo,ResponsavelId,CriadoEm,AtualizadoEm)
                    VALUES ({topicId}, 'Tópico coletivo legado', 'Contexto coletivo preservado sem atribuir os relatos vinculados ao responsável.',
                            {1}, 'São Paulo', 'Bairro original', 'Praça do agrupamento', {4}, {true}, {adminId}, {storedNow}, {storedNow});
                    """);
                await InsertReport(db, isolatedReportId, userId, null, false, "Relato isolado preservado integralmente.", "Local isolado", storedNow);
                await InsertReport(db, linkedReportA, userId, topicId, false, "Contribuição vinculada da pessoa legada.", "Praça do agrupamento", storedNow);
                await InsertReport(db, linkedReportB, adminId, topicId, true, "Contribuição vinculada e moderada de outro autor.", "Praça do agrupamento", storedNow);

                await InsertEvidence(db, Guid.NewGuid(), isolatedReportId, userId, "https://example.invalid/isolada.jpg", storedNow);
                await InsertEvidence(db, Guid.NewGuid(), linkedReportA, userId, "https://example.invalid/a.jpg", storedNow);
                await InsertEvidence(db, Guid.NewGuid(), linkedReportA, userId, "https://example.invalid/b.jpg", storedNow);
                const string dossierSnapshot = "{\"versao\":1,\"conteudo\":\"preservado\"}";
                await db.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO Apoios (Id,TopicoId,UsuarioId,CriadoEm)
                    VALUES ({supportId},{topicId},{userId},{storedNow});
                    INSERT INTO Participacoes
                        (Id,TopicoId,AutorId,Tipo,Natureza,Texto,Demonstrativo,CriadoEm,Oculta,MotivoModeracao,ModeradaPorId,ModeradaEm)
                    VALUES ({visibleParticipationId},{topicId},{userId},{3},{3},'Alternativa legada visível.',{true},{storedNow},{false},NULL,NULL,NULL),
                           ({hiddenParticipationId},{topicId},{userId},{2},{1},'Texto legado oculto preservado.',{true},{storedNow},{true},
                            'Continha dado pessoal.',{adminId},{storedNow});
                    INSERT INTO Dossies (Id,TopicoId,Versao,AutorId,SnapshotJson,Demonstrativo,CriadoEm)
                    VALUES ({dossierId},{topicId},{1},{adminId},{dossierSnapshot},{true},{storedNow});
                    INSERT INTO Acompanhamentos
                        (Id,TopicoId,AutorId,SituacaoAnterior,SituacaoNova,Descricao,Origem,Protocolo,Simulado,CriadoEm)
                    VALUES ({compatibleTrackingId},{topicId},{adminId},{1},{2},'Dossiê consolidado.','Fluxo legado',NULL,{true},{storedNow}),
                           ({preservedTrackingId},{topicId},{adminId},{3},{4},'Resultado apenas simulado.','Fluxo legado',
                            'SIMULADO',{true},{storedNow});
                    """);

                await migrator.MigrateAsync();
                Assert.Equal(2, await db.Demandas.CountAsync());
                Assert.Equal(3, await RawCountAsync(db, "Relatos"));
                Assert.Equal(3, await RawCountAsync(db, "Evidencias"));
                Assert.Equal(2, await db.Comentarios.CountAsync());
                Assert.Equal(1, await db.Apoios.CountAsync());
                Assert.Equal(1, await db.HistoricosSituacao.CountAsync());
                Assert.Equal(15, await db.CorrespondenciasLegadas.CountAsync());
                Assert.Equal(userHash, (await db.Users.SingleAsync(x => x.Id == userId)).PasswordHash);

                var collective = await db.Demandas.SingleAsync(x => x.Id == topicId);
                Assert.Equal(adminId, collective.AutorId);
                Assert.Equal(SituacaoDemanda.EmAndamento, collective.Situacao);
                Assert.Null(collective.EvidenciaUrl);
                var isolated = await db.Demandas.SingleAsync(x => x.Id == isolatedReportId);
                Assert.Equal(userId, isolated.AutorId);
                Assert.Equal("https://example.invalid/isolada.jpg", isolated.EvidenciaUrl);
                var hidden = await db.Comentarios.SingleAsync(x => x.Id == hiddenParticipationId);
                Assert.True(hidden.Oculto);
                Assert.Equal("Texto legado oculto preservado.", hidden.Texto);
            }

            using (var factory = new AjaFactory(path, deleteDatabaseOnDispose: false, enableAdminBootstrap: false))
            using (var client = factory.CreateClient())
            {
                var token = await client.LoginAsync(userEmail, "User#Legacy2026!");
                client.Bearer(token);
                var comments = await client.GetFromJsonAsync<JsonElement>($"/api/v1/demandas/{topicId}/comentarios");
                Assert.Equal(1, comments.GetProperty("totalItems").GetInt32());
            }

            await using (var verification = new AjaDbContext(options))
            {
                await verification.Database.MigrateAsync();
                Assert.Equal(2, await verification.Demandas.CountAsync());
                Assert.Equal(2, await verification.Comentarios.CountAsync());
                Assert.Equal(15, await verification.CorrespondenciasLegadas.CountAsync());
            }
        }
        finally
        {
            AjaFactory.DeleteDatabaseFiles(path);
        }
    }

    private static ApplicationUser NewUser(string id, string email, string alias) => new()
    {
        Id = id,
        UserName = email,
        NormalizedUserName = email.ToUpperInvariant(),
        Email = email,
        NormalizedEmail = email.ToUpperInvariant(),
        Alias = alias,
        EmailConfirmed = true,
        SecurityStamp = Guid.NewGuid().ToString(),
        ConcurrencyStamp = Guid.NewGuid().ToString(),
        LockoutEnabled = true
    };

    private static long Store(DateTimeOffset value) =>
        new DateTimeOffsetToBinaryConverter().ConvertToProviderExpression.Compile()(value);

    private static Task<int> RawCountAsync(AjaDbContext db, string table) =>
        table switch
        {
            "Relatos" => db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM Relatos").SingleAsync(),
            "Evidencias" => db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM Evidencias").SingleAsync(),
            _ => throw new ArgumentOutOfRangeException(nameof(table))
        };

    private static Task<int> InsertReport(
        AjaDbContext db,
        Guid id,
        string authorId,
        Guid? topicId,
        bool hidden,
        string description,
        string location,
        long storedNow) =>
        db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO Relatos
                (Id,AutorId,Titulo,Descricao,Categoria,Cidade,Bairro,ReferenciaLocal,Latitude,Longitude,Demonstrativo,
                 TopicoId,Oculto,MotivoModeracao,ModeradoPorId,ModeradoEm,CriadoEm,AtualizadoEm)
            VALUES ({id},{authorId},'Título legado', {description},{1},'São Paulo','Bairro preservado',{location},NULL,NULL,{true},
                    {topicId},{hidden},{(hidden ? "Motivo legado preservado." : null)},{(hidden ? "legacy-admin" : null)},
                    {(hidden ? storedNow : null)},{storedNow},{storedNow});
            """);

    private static Task<int> InsertEvidence(
        AjaDbContext db,
        Guid id,
        Guid reportId,
        string authorId,
        string url,
        long storedNow) =>
        db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO Evidencias
                (Id,RelatoId,AutorId,Descricao,Tipo,Procedencia,Url,DataObservacao,Demonstrativo,CriadoEm)
            VALUES ({id},{reportId},{authorId},'Evidência legada preservada.',{4},'Fonte legada', {url},NULL,{true},{storedNow});
            """);
}
