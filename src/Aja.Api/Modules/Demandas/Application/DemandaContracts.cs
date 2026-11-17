using System.ComponentModel.DataAnnotations;
using Aja.Api.Modules.Demandas.Domain;
using Aja.Api.Shared.Configuration;

namespace Aja.Api.Modules.Demandas.Application;

public sealed record CriarDemandaRequest(
    [param: Required, StringLength(5000, MinimumLength = 10)] string Descricao,
    [param: Required, StringLength(500, MinimumLength = 5)] string Local,
    [param: StringLength(2048)] string? EvidenciaUrl = null);

public sealed record EditarDemandaRequest(
    [param: Required, StringLength(5000, MinimumLength = 10)] string Descricao,
    [param: Required, StringLength(500, MinimumLength = 5)] string Local,
    [param: StringLength(2048)] string? EvidenciaUrl = null);

public sealed record AtualizarDemandaAdminRequest(
    CategoriaDemanda? Categoria = null,
    SituacaoDemanda? Situacao = null,
    [param: StringLength(2000, MinimumLength = 5)] string? Justificativa = null);

public sealed record DemandaResumoResponse(
    Guid Id,
    string Descricao,
    string Local,
    string? EvidenciaUrl,
    string AutorId,
    string AutorAlias,
    string Cidade,
    string Bairro,
    CategoriaDemanda? Categoria,
    string CategoriaRotulo,
    SituacaoDemanda Situacao,
    int TotalApoios,
    int TotalComentarios,
    DateTimeOffset CriadoEm,
    DateTimeOffset AtualizadoEm);

public sealed record HistoricoSituacaoResponse(
    Guid Id,
    SituacaoDemanda SituacaoAnterior,
    SituacaoDemanda SituacaoNova,
    string Justificativa,
    string ResponsavelId,
    string ResponsavelAlias,
    DateTimeOffset CriadoEm);

public sealed record DemandaDetalheResponse(
    DemandaResumoResponse Demanda,
    bool ApoiadaPeloUsuarioAtual,
    IReadOnlyList<HistoricoSituacaoResponse> Historico);

public interface IDemandaService
{
    Task<DemandaDetalheResponse> CreateAsync(CriarDemandaRequest request, CancellationToken cancellationToken);
    Task<PagedResponse<DemandaResumoResponse>> ListAsync(
        SituacaoDemanda? situacao,
        CategoriaDemanda? categoria,
        string? bairro,
        bool minhas,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
    Task<DemandaDetalheResponse> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<DemandaDetalheResponse> UpdateAsync(Guid id, EditarDemandaRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<DemandaDetalheResponse> UpdateAdministrationAsync(Guid id, AtualizarDemandaAdminRequest request, CancellationToken cancellationToken);
}
