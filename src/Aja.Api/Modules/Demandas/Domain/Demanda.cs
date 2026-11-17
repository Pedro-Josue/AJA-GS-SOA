using Aja.Api.Shared.Errors;

namespace Aja.Api.Modules.Demandas.Domain;

public enum CategoriaDemanda
{
    Fisica = 1,
    Visual = 2,
    Auditiva = 3,
    Comunicacional = 4,
    Cognitiva = 5
}

public enum SituacaoDemanda
{
    Aberta = 1,
    EmAndamento = 2,
    Concluida = 3
}

public enum VisibilidadeDemanda
{
    Publica = 1,
    RestritaAoAutorEAdmin = 2,
    HistoricaSomenteAdmin = 3
}

public sealed class Demanda
{
    private Demanda() { }

    public Demanda(
        string autorId,
        string descricao,
        string local,
        string? evidenciaUrl,
        string cidade,
        string bairro,
        DateTimeOffset agora)
    {
        Id = Guid.NewGuid();
        AutorId = Required(autorId, "autor");
        Cidade = InRange(cidade, 2, 100, "cidade");
        Bairro = InRange(bairro, 2, 100, "bairro");
        Situacao = SituacaoDemanda.Aberta;
        Visibilidade = VisibilidadeDemanda.Publica;
        CriadoEm = agora;
        AplicarConteudo(descricao, local, evidenciaUrl, agora);
    }

    public Guid Id { get; private set; }
    public string Descricao { get; private set; } = string.Empty;
    public string Local { get; private set; } = string.Empty;
    public string? EvidenciaUrl { get; private set; }
    public string AutorId { get; private set; } = string.Empty;
    public string Cidade { get; private set; } = string.Empty;
    public string Bairro { get; private set; } = string.Empty;
    public CategoriaDemanda? Categoria { get; private set; }
    public SituacaoDemanda Situacao { get; private set; }
    public VisibilidadeDemanda Visibilidade { get; private set; }
    public bool Demonstrativa { get; private set; }
    public string? OrigemLegada { get; private set; }
    public bool Removida { get; private set; }
    public string? RemovidaPorId { get; private set; }
    public DateTimeOffset? RemovidaEm { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }
    public DateTimeOffset AtualizadoEm { get; private set; }

    public void Editar(string descricao, string local, string? evidenciaUrl, DateTimeOffset agora)
    {
        GarantirAtiva();
        if (Situacao != SituacaoDemanda.Aberta)
            throw AjaProblemException.Conflict("Somente uma demanda Aberta pode ser editada.", "demanda_nao_aberta");
        AplicarConteudo(descricao, local, evidenciaUrl, agora);
    }

    public HistoricoSituacao? AtualizarAdministracao(
        CategoriaDemanda? categoria,
        SituacaoDemanda? novaSituacao,
        string? justificativa,
        string responsavelId,
        DateTimeOffset agora)
    {
        GarantirAtiva();
        var categoriaAlterada = false;
        if (categoria.HasValue)
        {
            if (!Enum.IsDefined(categoria.Value)) throw AjaProblemException.BadRequest("Categoria inválida.");
            if (Categoria != categoria.Value)
            {
                Categoria = categoria.Value;
                categoriaAlterada = true;
            }
        }

        if (!novaSituacao.HasValue || novaSituacao.Value == Situacao)
        {
            if (categoriaAlterada) AtualizadoEm = agora;
            return null;
        }
        if (!Enum.IsDefined(novaSituacao.Value)) throw AjaProblemException.BadRequest("Situação inválida.");
        var motivo = InRange(justificativa ?? string.Empty, 5, 2000, "justificativa");
        var anterior = Situacao;
        Situacao = novaSituacao.Value;
        AtualizadoEm = agora;
        return new HistoricoSituacao(Id, anterior, Situacao, motivo, responsavelId, agora);
    }

    public void Remover(string autorId, DateTimeOffset agora)
    {
        GarantirAtiva();
        if (Situacao != SituacaoDemanda.Aberta)
            throw AjaProblemException.Conflict("Somente uma demanda Aberta pode ser removida.", "demanda_nao_aberta");
        Removida = true;
        RemovidaPorId = Required(autorId, "autor");
        RemovidaEm = agora;
        AtualizadoEm = agora;
    }

    private void AplicarConteudo(string descricao, string local, string? evidenciaUrl, DateTimeOffset agora)
    {
        Descricao = InRange(descricao, 10, 5000, "descrição");
        Local = InRange(local, 5, 500, "local");
        EvidenciaUrl = OptionalHttpUrl(evidenciaUrl);
        AtualizadoEm = agora;
    }

    private void GarantirAtiva()
    {
        if (Removida) throw AjaProblemException.NotFound("Demanda não encontrada.");
    }

    private static string? OptionalHttpUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var text = value.Trim();
        if (text.Length > 2048 || !Uri.TryCreate(text, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            throw AjaProblemException.BadRequest("A URL de evidência deve ser HTTP ou HTTPS.");
        return text;
    }

    private static string Required(string value, string field) =>
        string.IsNullOrWhiteSpace(value)
            ? throw AjaProblemException.BadRequest($"{field} é obrigatório.")
            : value.Trim();

    private static string InRange(string value, int min, int max, string field)
    {
        var text = Required(value, field);
        if (text.Length < min || text.Length > max)
            throw AjaProblemException.BadRequest($"{field} deve ter entre {min} e {max} caracteres.");
        return text;
    }
}

public sealed class HistoricoSituacao
{
    private HistoricoSituacao() { }

    public HistoricoSituacao(
        Guid demandaId,
        SituacaoDemanda situacaoAnterior,
        SituacaoDemanda situacaoNova,
        string justificativa,
        string responsavelId,
        DateTimeOffset criadoEm)
    {
        Id = Guid.NewGuid();
        DemandaId = demandaId;
        SituacaoAnterior = situacaoAnterior;
        SituacaoNova = situacaoNova;
        Justificativa = justificativa;
        ResponsavelId = responsavelId;
        CriadoEm = criadoEm;
    }

    public Guid Id { get; private set; }
    public Guid DemandaId { get; private set; }
    public SituacaoDemanda SituacaoAnterior { get; private set; }
    public SituacaoDemanda SituacaoNova { get; private set; }
    public string Justificativa { get; private set; } = string.Empty;
    public string ResponsavelId { get; private set; } = string.Empty;
    public DateTimeOffset CriadoEm { get; private set; }
    public string? OrigemLegada { get; private set; }
    public bool Demonstrativo { get; private set; }
}
