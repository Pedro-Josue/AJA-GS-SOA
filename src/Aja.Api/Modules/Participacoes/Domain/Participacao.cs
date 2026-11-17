using Aja.Api.Shared.Errors;

namespace Aja.Api.Modules.Participacoes.Domain;

public sealed class Apoio
{
    private Apoio() { }

    public Apoio(Guid demandaId, string usuarioId, DateTimeOffset agora)
    {
        Id = Guid.NewGuid();
        DemandaId = demandaId;
        UsuarioId = usuarioId;
        Ativo = true;
        CriadoEm = agora;
        AtualizadoEm = agora;
    }

    public Guid Id { get; private set; }
    public Guid DemandaId { get; private set; }
    public string UsuarioId { get; private set; } = string.Empty;
    public bool Ativo { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }
    public DateTimeOffset AtualizadoEm { get; private set; }
    public string? OrigemLegada { get; private set; }

    public void Registrar(DateTimeOffset agora)
    {
        if (Ativo) return;
        Ativo = true;
        AtualizadoEm = agora;
    }

    public void Retirar(DateTimeOffset agora)
    {
        if (!Ativo) return;
        Ativo = false;
        AtualizadoEm = agora;
    }
}

public sealed class Comentario
{
    private Comentario() { }

    public Comentario(Guid demandaId, string autorId, string texto, DateTimeOffset agora)
    {
        var conteudo = texto?.Trim() ?? string.Empty;
        if (conteudo.Length is < 3 or > 3000)
            throw AjaProblemException.BadRequest("O comentário deve ter entre 3 e 3.000 caracteres.");
        Id = Guid.NewGuid();
        DemandaId = demandaId;
        AutorId = autorId;
        Texto = conteudo;
        CriadoEm = agora;
    }

    public Guid Id { get; private set; }
    public Guid DemandaId { get; private set; }
    public string AutorId { get; private set; } = string.Empty;
    public string Texto { get; private set; } = string.Empty;
    public DateTimeOffset CriadoEm { get; private set; }
    public bool Removido { get; private set; }
    public string? RemovidoPorId { get; private set; }
    public DateTimeOffset? RemovidoEm { get; private set; }
    public bool Oculto { get; private set; }
    public string? MotivoModeracao { get; private set; }
    public string? ModeradoPorId { get; private set; }
    public DateTimeOffset? ModeradoEm { get; private set; }
    public string? TipoLegado { get; private set; }
    public string? NaturezaLegada { get; private set; }
    public bool Demonstrativo { get; private set; }
    public string? OrigemLegada { get; private set; }

    public void Remover(string autorId, DateTimeOffset agora)
    {
        if (Removido) return;
        Removido = true;
        RemovidoPorId = autorId;
        RemovidoEm = agora;
    }

    public void Ocultar(string motivo, string adminId, DateTimeOffset agora)
    {
        var justificativa = motivo?.Trim() ?? string.Empty;
        if (justificativa.Length is < 5 or > 2000)
            throw AjaProblemException.BadRequest("O motivo deve ter entre 5 e 2.000 caracteres.");
        if (Oculto) return;
        Oculto = true;
        MotivoModeracao = justificativa;
        ModeradoPorId = adminId;
        ModeradoEm = agora;
    }
}
