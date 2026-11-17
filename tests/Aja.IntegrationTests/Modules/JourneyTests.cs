using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Aja.IntegrationTests.Modules;

public sealed class JourneyTests
{
    [Fact]
    public async Task JornadaSimplificada_CobreDemandaApoioComentarioHistoricoEModeracao()
    {
        using var factory = new AjaFactory();
        using var client = factory.CreateClient();
        var (_, authorToken) = await client.RegisterAndLoginAsync("Moradora A");
        var (_, participantToken) = await client.RegisterAndLoginAsync("Morador B");
        var adminToken = await client.LoginAsync("admin@aja.test", "Admin#Aja2026!");

        client.Bearer(authorToken);
        var create = await client.PostAsJsonAsync("/api/v1/demandas", new
        {
            descricao = "Entulho bloqueia a calçada e dificulta a passagem.",
            local = "Em frente à entrada da praça, na Rua Exemplo."
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Assert.NotNull(create.Headers.Location);
        var created = await create.Content.ReadFromJsonAsync<JsonElement>();
        var demandId = created.GetProperty("demanda").GetProperty("id").GetGuid();
        Assert.Equal("São Paulo", created.GetProperty("demanda").GetProperty("cidade").GetString());
        Assert.Equal("Doroteia", created.GetProperty("demanda").GetProperty("bairro").GetString());
        Assert.Equal("A classificar", created.GetProperty("demanda").GetProperty("categoriaRotulo").GetString());
        Assert.Equal("Aberta", created.GetProperty("demanda").GetProperty("situacao").GetString());

        client.Bearer(participantToken);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsync($"/api/v1/demandas/{demandId}/apoio", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsync($"/api/v1/demandas/{demandId}/apoio", null)).StatusCode);
        var comment = await client.PostAsJsonAsync($"/api/v1/demandas/{demandId}/comentarios", new
        {
            texto = "Sugiro retirar o material e liberar a faixa de circulação.",
            tipo = "Alternativa",
            autorId = Guid.NewGuid()
        });
        Assert.Equal(HttpStatusCode.Created, comment.StatusCode);
        var commentId = (await comment.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var detail = await client.GetFromJsonAsync<JsonElement>($"/api/v1/demandas/{demandId}");
        Assert.Equal(1, detail.GetProperty("demanda").GetProperty("totalApoios").GetInt32());
        Assert.Equal(1, detail.GetProperty("demanda").GetProperty("totalComentarios").GetInt32());

        client.Bearer(adminToken);
        var classify = await client.PatchAsJsonAsync($"/api/v1/demandas/{demandId}", new { categoria = "Fisica" });
        Assert.Equal(HttpStatusCode.OK, classify.StatusCode);
        var progress = await client.PatchAsJsonAsync($"/api/v1/demandas/{demandId}", new
        {
            situacao = "EmAndamento",
            justificativa = "A demanda está em análise pela administração."
        });
        Assert.Equal(HttpStatusCode.OK, progress.StatusCode);
        var repeated = await client.PatchAsJsonAsync($"/api/v1/demandas/{demandId}", new { situacao = "EmAndamento" });
        Assert.Equal(HttpStatusCode.OK, repeated.StatusCode);
        var conclude = await client.PatchAsJsonAsync($"/api/v1/demandas/{demandId}", new
        {
            situacao = "Concluida",
            justificativa = "O entulho foi retirado e a faixa de circulação ficou livre."
        });
        Assert.Equal(HttpStatusCode.OK, conclude.StatusCode);
        var concluded = await conclude.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, concluded.GetProperty("historico").GetArrayLength());

        client.Bearer(authorToken);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/v1/demandas/{demandId}", new
        {
            descricao = "Tentativa de editar depois da conclusão.",
            local = "Em frente à entrada da praça"
        })).StatusCode);
        var afterConclusion = await client.PostAsJsonAsync($"/api/v1/demandas/{demandId}/comentarios", new
        {
            texto = "O resultado informado pode ser acompanhado pela comunidade."
        });
        Assert.Equal(HttpStatusCode.Created, afterConclusion.StatusCode);
        var authorCommentId = (await afterConclusion.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.Forbidden, (await client.PatchAsJsonAsync($"/api/v1/comentarios/{commentId}/moderacao", new
        {
            motivo = "Usuário comum não pode moderar comentário."
        })).StatusCode);
        client.Bearer(adminToken);
        Assert.Equal(HttpStatusCode.OK, (await client.PatchAsJsonAsync($"/api/v1/comentarios/{commentId}/moderacao", new
        {
            motivo = "Comentário ocultado por conter informação pessoal de terceiros."
        })).StatusCode);

        client.Bearer(authorToken);
        var visible = await client.GetFromJsonAsync<JsonElement>($"/api/v1/demandas/{demandId}/comentarios");
        Assert.Equal(1, visible.GetProperty("totalItems").GetInt32());
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/comentarios/{authorCommentId}")).StatusCode);
        var noneVisible = await client.GetFromJsonAsync<JsonElement>($"/api/v1/demandas/{demandId}/comentarios");
        Assert.Equal(0, noneVisible.GetProperty("totalItems").GetInt32());
    }

    [Fact]
    public async Task ExclusaoLogica_ExigeAutoriaAberturaEAusenciaDeParticipacao()
    {
        using var factory = new AjaFactory();
        using var client = factory.CreateClient();
        var (_, authorToken) = await client.RegisterAndLoginAsync("Autora");
        var (_, otherToken) = await client.RegisterAndLoginAsync("Participante");

        client.Bearer(authorToken);
        var removable = await client.CreateDemandAsync("Demanda sem participações para remoção.", "Local público removível");
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/demandas/{removable}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/demandas/{removable}")).StatusCode);

        var supported = await client.CreateDemandAsync("Demanda que receberá um apoio comunitário.", "Local público com apoio");
        client.Bearer(otherToken);
        await client.PutAsync($"/api/v1/demandas/{supported}/apoio", null);
        await client.DeleteAsync($"/api/v1/demandas/{supported}/apoio");
        client.Bearer(authorToken);
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/v1/demandas/{supported}")).StatusCode);

        var commented = await client.CreateDemandAsync("Demanda que receberá comentário comunitário.", "Local público comentado");
        client.Bearer(otherToken);
        var comment = await client.PostAsJsonAsync($"/api/v1/demandas/{commented}/comentarios", new { texto = "Comentário histórico." });
        var commentId = (await comment.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await client.DeleteAsync($"/api/v1/comentarios/{commentId}");
        client.Bearer(authorToken);
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/v1/demandas/{commented}")).StatusCode);
    }

    [Fact]
    public async Task FiltrosPaginacaoELeituraDoResumo_SaoDeterministicosENaoGravamHistorico()
    {
        using var factory = new AjaFactory();
        using var client = factory.CreateClient();
        var (_, userToken) = await client.RegisterAndLoginAsync("Pessoa filtros");
        var adminToken = await client.LoginAsync("admin@aja.test", "Admin#Aja2026!");
        client.Bearer(userToken);
        var first = await client.CreateDemandAsync("Primeira barreira física para o filtro.", "Local A em Doroteia");
        _ = await client.CreateDemandAsync("Segunda barreira ainda não classificada.", "Local B em Doroteia");

        client.Bearer(adminToken);
        await client.PatchAsJsonAsync($"/api/v1/demandas/{first}", new
        {
            categoria = "Fisica",
            situacao = "EmAndamento",
            justificativa = "Classificação e análise iniciadas pela administração."
        });
        client.Bearer(userToken);
        var filtered = await client.GetFromJsonAsync<JsonElement>(
            "/api/v1/demandas?categoria=Fisica&situacao=EmAndamento&bairro=Doroteia&minhas=true&page=1&pageSize=1");
        Assert.Equal(1, filtered.GetProperty("totalItems").GetInt32());
        Assert.Equal(first, filtered.GetProperty("items")[0].GetProperty("id").GetGuid());

        var before = await client.GetFromJsonAsync<JsonElement>($"/api/v1/demandas/{first}");
        var after = await client.GetFromJsonAsync<JsonElement>($"/api/v1/demandas/{first}");
        Assert.Equal(before.GetProperty("historico").GetArrayLength(), after.GetProperty("historico").GetArrayLength());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/demandas?page=0&pageSize=101")).StatusCode);
    }
}
