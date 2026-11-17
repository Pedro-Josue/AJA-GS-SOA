using Aja.Api.Modules.Demandas.Domain;
using Aja.Api.Modules.Participacoes.Domain;
using Aja.Api.Shared.Errors;

namespace Aja.UnitTests.Modules;

public sealed class DomainRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 14, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Demanda_CriaComDoisCamposEPadroesDoServidor()
    {
        var demanda = NewDemand();

        Assert.Equal("Barreira na calçada", demanda.Descricao);
        Assert.Equal("Em frente à praça", demanda.Local);
        Assert.Null(demanda.Categoria);
        Assert.Equal(SituacaoDemanda.Aberta, demanda.Situacao);
        Assert.Equal("São Paulo", demanda.Cidade);
        Assert.Equal("Doroteia", demanda.Bairro);
    }

    [Theory]
    [InlineData("file:///foto.jpg")]
    [InlineData("ftp://exemplo.test/doc")]
    [InlineData("sem-esquema")]
    public void Demanda_RejeitaEvidenciaQueNaoSejaHttp(string url)
    {
        var error = Assert.Throws<AjaProblemException>(() => new Demanda(
            "autor", "Barreira na calçada", "Em frente à praça", url, "São Paulo", "Doroteia", Now));
        Assert.Equal(400, error.StatusCode);
    }

    [Fact]
    public void Demanda_MudancaDeSituacaoGeraHistoricoEImpedeEdicao()
    {
        var demanda = NewDemand();
        var history = demanda.AtualizarAdministracao(CategoriaDemanda.Fisica, SituacaoDemanda.EmAndamento,
            "A demanda está em análise.", "admin", Now.AddMinutes(1));

        Assert.NotNull(history);
        Assert.Equal(SituacaoDemanda.Aberta, history!.SituacaoAnterior);
        Assert.Equal(SituacaoDemanda.EmAndamento, history.SituacaoNova);
        Assert.Equal(CategoriaDemanda.Fisica, demanda.Categoria);
        var edit = Assert.Throws<AjaProblemException>(() => demanda.Editar(
            "Descrição atualizada", "Outro local válido", null, Now.AddMinutes(2)));
        Assert.Equal("demanda_nao_aberta", edit.Code);
    }

    [Fact]
    public void Demanda_StatusRepetidoNaoDuplicaHistoricoEReaberturaFunciona()
    {
        var demanda = NewDemand();
        _ = demanda.AtualizarAdministracao(null, SituacaoDemanda.Concluida,
            "Resultado registrado pela administração.", "admin", Now.AddMinutes(1));

        Assert.Null(demanda.AtualizarAdministracao(null, SituacaoDemanda.Concluida, null, "admin", Now.AddMinutes(2)));
        var reopen = demanda.AtualizarAdministracao(null, SituacaoDemanda.Aberta,
            "O problema voltou a ocorrer no local.", "admin", Now.AddMinutes(3));

        Assert.NotNull(reopen);
        Assert.Equal(SituacaoDemanda.Aberta, demanda.Situacao);
    }

    [Fact]
    public void Demanda_MudancaEfetivaExigeJustificativa()
    {
        var demanda = NewDemand();
        var error = Assert.Throws<AjaProblemException>(() => demanda.AtualizarAdministracao(
            null, SituacaoDemanda.EmAndamento, null, "admin", Now));
        Assert.Equal(400, error.StatusCode);
    }

    [Fact]
    public void Apoio_RetiradaEReativacaoSaoIdempotentes()
    {
        var apoio = new Apoio(Guid.NewGuid(), "usuario", Now);
        apoio.Registrar(Now.AddMinutes(1));
        apoio.Retirar(Now.AddMinutes(2));
        apoio.Retirar(Now.AddMinutes(3));
        apoio.Registrar(Now.AddMinutes(4));

        Assert.True(apoio.Ativo);
        Assert.Equal(Now.AddMinutes(4), apoio.AtualizadoEm);
    }

    [Fact]
    public void Comentario_PreservaTextoAoRemoverEModeracaoNaoTrocaPrimeiroAdmin()
    {
        var comentario = new Comentario(Guid.NewGuid(), "autor", "Sugestão comunitária.", Now);
        comentario.Ocultar("Contém dado pessoal de terceiro.", "admin-1", Now.AddMinutes(1));
        comentario.Ocultar("Tentativa de substituir a moderação.", "admin-2", Now.AddMinutes(2));
        comentario.Remover("autor", Now.AddMinutes(3));

        Assert.True(comentario.Oculto);
        Assert.True(comentario.Removido);
        Assert.Equal("Sugestão comunitária.", comentario.Texto);
        Assert.Equal("admin-1", comentario.ModeradoPorId);
        Assert.Equal("Contém dado pessoal de terceiro.", comentario.MotivoModeracao);
    }

    private static Demanda NewDemand() => new(
        "autor", " Barreira na calçada ", " Em frente à praça ", null, "São Paulo", "Doroteia", Now);
}
