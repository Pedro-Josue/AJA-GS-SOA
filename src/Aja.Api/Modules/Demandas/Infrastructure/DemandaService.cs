using Aja.Api.Modules.Auth.Application;
using Aja.Api.Modules.Demandas.Application;
using Aja.Api.Modules.Demandas.Domain;
using Aja.Api.Shared.Configuration;
using Aja.Api.Shared.Errors;
using Aja.Api.Shared.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Aja.Api.Modules.Demandas.Infrastructure;

public sealed class DemandaService(
    AjaDbContext db,
    ICurrentUser currentUser,
    IOptions<PilotoOptions> piloto,
    TimeProvider clock,
    ILogger<DemandaService> logger) : IDemandaService
{
    public async Task<DemandaDetalheResponse> CreateAsync(CriarDemandaRequest request, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var options = piloto.Value;
        var demanda = new Demanda(currentUser.Id, request.Descricao, request.Local, request.EvidenciaUrl,
            options.Cidade, options.Bairro, now);
        db.Demandas.Add(demanda);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(demanda.Id, cancellationToken);
    }

    public async Task<PagedResponse<DemandaResumoResponse>> ListAsync(
        SituacaoDemanda? situacao,
        CategoriaDemanda? categoria,
        string? bairro,
        bool minhas,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        ValidatePage(page, pageSize);
        var query = VisibleDemandas().AsNoTracking();
        if (situacao.HasValue) query = query.Where(x => x.Situacao == situacao.Value);
        if (categoria.HasValue) query = query.Where(x => x.Categoria == categoria.Value);
        if (!string.IsNullOrWhiteSpace(bairro)) query = query.Where(x => x.Bairro == bairro.Trim());
        if (minhas) query = query.Where(x => x.AutorId == currentUser.Id);

        var total = await query.CountAsync(cancellationToken);
        var pageQuery = query.OrderByDescending(x => x.CriadoEm).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize);
        var items = await ProjectSummaries(pageQuery)
            .ToListAsync(cancellationToken);
        return new PagedResponse<DemandaResumoResponse>(items, page, pageSize, total);
    }

    public async Task<DemandaDetalheResponse> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var summary = await ProjectSummaries(VisibleDemandas().AsNoTracking().Where(x => x.Id == id))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw AjaProblemException.NotFound("Demanda não encontrada.");
        var supported = await db.Apoios.AsNoTracking()
            .AnyAsync(x => x.DemandaId == id && x.UsuarioId == currentUser.Id && x.Ativo, cancellationToken);
        var history = await db.HistoricosSituacao.AsNoTracking().Where(x => x.DemandaId == id)
            .OrderBy(x => x.CriadoEm).ThenBy(x => x.Id)
            .Select(x => new HistoricoSituacaoResponse(
                x.Id, x.SituacaoAnterior, x.SituacaoNova, x.Justificativa, x.ResponsavelId,
                db.Users.Where(u => u.Id == x.ResponsavelId).Select(u => u.Alias).Single(), x.CriadoEm))
            .ToListAsync(cancellationToken);
        return new DemandaDetalheResponse(summary, supported, history);
    }

    public async Task<DemandaDetalheResponse> UpdateAsync(Guid id, EditarDemandaRequest request, CancellationToken cancellationToken)
    {
        var demanda = await FindVisibleAsync(id, cancellationToken);
        EnsureOwner(demanda);
        demanda.Editar(request.Descricao, request.Local, request.EvidenciaUrl, clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var demanda = await FindVisibleAsync(id, cancellationToken);
        EnsureOwner(demanda);
        var idText = id.ToString();
        var hasParticipation = await db.Apoios.AnyAsync(x => x.DemandaId == id, cancellationToken)
            || await db.Comentarios.AnyAsync(x => x.DemandaId == id, cancellationToken)
            || await db.CorrespondenciasLegadas.AnyAsync(x =>
                x.DestinoTipo == "Demanda" && x.DestinoId == idText && x.OrigemTipo == "Relato" &&
                x.Tratamento == "Preservado no agrupamento do tópico", cancellationToken);
        if (hasParticipation)
            throw AjaProblemException.Conflict("A demanda possui participação e deve ser preservada.", "demanda_com_participacao");
        demanda.Remover(currentUser.Id, clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<DemandaDetalheResponse> UpdateAdministrationAsync(
        Guid id,
        AtualizarDemandaAdminRequest request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAdmin) throw AjaProblemException.Forbidden();
        if (!request.Categoria.HasValue && !request.Situacao.HasValue)
            throw AjaProblemException.BadRequest("Informe categoria e/ou situação.");
        var demanda = await db.Demandas.SingleOrDefaultAsync(x => x.Id == id && !x.Removida, cancellationToken)
            ?? throw AjaProblemException.NotFound("Demanda não encontrada.");
        var history = demanda.AtualizarAdministracao(request.Categoria, request.Situacao, request.Justificativa,
            currentUser.Id, clock.GetUtcNow());
        if (history is not null) db.HistoricosSituacao.Add(history);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Admin {AdminId} atualizou demanda {DemandaId}; categoria={Categoria}; situacao={Situacao}; utc={Utc}",
            currentUser.Id, id, request.Categoria, request.Situacao, clock.GetUtcNow());
        return await GetAsync(id, cancellationToken);
    }

    private IQueryable<Demanda> VisibleDemandas()
    {
        var query = db.Demandas.Where(x => !x.Removida);
        if (currentUser.IsAdmin) return query;
        return query.Where(x => x.Visibilidade == VisibilidadeDemanda.Publica ||
            (x.Visibilidade == VisibilidadeDemanda.RestritaAoAutorEAdmin && x.AutorId == currentUser.Id));
    }

    private async Task<Demanda> FindVisibleAsync(Guid id, CancellationToken cancellationToken) =>
        await VisibleDemandas().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
        ?? throw AjaProblemException.NotFound("Demanda não encontrada.");

    private void EnsureOwner(Demanda demanda)
    {
        if (demanda.AutorId != currentUser.Id)
            throw AjaProblemException.Forbidden("Somente o autor pode alterar esta demanda.");
    }

    private IQueryable<DemandaResumoResponse> ProjectSummaries(IQueryable<Demanda> query) =>
        query.Select(x => new DemandaResumoResponse(
            x.Id, x.Descricao, x.Local, x.EvidenciaUrl, x.AutorId,
            db.Users.Where(u => u.Id == x.AutorId).Select(u => u.Alias).Single(),
            x.Cidade, x.Bairro, x.Categoria,
            x.Categoria == null ? "A classificar" :
                x.Categoria == CategoriaDemanda.Fisica ? "Física" :
                x.Categoria == CategoriaDemanda.Visual ? "Visual" :
                x.Categoria == CategoriaDemanda.Auditiva ? "Auditiva" :
                x.Categoria == CategoriaDemanda.Comunicacional ? "Comunicacional" : "Cognitiva",
            x.Situacao,
            db.Apoios.Count(a => a.DemandaId == x.Id && a.Ativo),
            db.Comentarios.Count(c => c.DemandaId == x.Id && !c.Removido && !c.Oculto),
            x.CriadoEm, x.AtualizadoEm));

    private static void ValidatePage(int page, int pageSize)
    {
        if (page < 1 || pageSize is < 1 or > 100)
            throw AjaProblemException.BadRequest("page deve ser >= 1 e pageSize deve estar entre 1 e 100.");
    }
}
