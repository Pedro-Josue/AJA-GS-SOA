using Aja.Api.Modules.Auth.Application;
using Aja.Api.Modules.Demandas.Domain;
using Aja.Api.Modules.Participacoes.Application;
using Aja.Api.Modules.Participacoes.Domain;
using Aja.Api.Shared.Configuration;
using Aja.Api.Shared.Errors;
using Aja.Api.Shared.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aja.Api.Modules.Participacoes.Infrastructure;

public sealed class ParticipacaoService(
    AjaDbContext db,
    ICurrentUser currentUser,
    TimeProvider clock,
    ILogger<ParticipacaoService> logger) : IParticipacaoService
{
    public async Task AddSupportAsync(Guid demandaId, CancellationToken cancellationToken)
    {
        await EnsureDemandVisibleAsync(demandaId, cancellationToken);
        var support = await db.Apoios.SingleOrDefaultAsync(
            x => x.DemandaId == demandaId && x.UsuarioId == currentUser.Id, cancellationToken);
        if (support is not null)
        {
            support.Registrar(clock.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        db.Apoios.Add(new Apoio(demandaId, currentUser.Id, clock.GetUtcNow()));
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            if (!await db.Apoios.AsNoTracking().AnyAsync(
                    x => x.DemandaId == demandaId && x.UsuarioId == currentUser.Id && x.Ativo, cancellationToken))
                throw;
        }
    }

    public async Task RemoveSupportAsync(Guid demandaId, CancellationToken cancellationToken)
    {
        await EnsureDemandVisibleAsync(demandaId, cancellationToken);
        var support = await db.Apoios.SingleOrDefaultAsync(
            x => x.DemandaId == demandaId && x.UsuarioId == currentUser.Id, cancellationToken);
        if (support is null) return;
        support.Retirar(clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ComentarioResponse> CreateCommentAsync(
        Guid demandaId,
        CriarComentarioRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureDemandVisibleAsync(demandaId, cancellationToken);
        var comment = new Comentario(demandaId, currentUser.Id, request.Texto, clock.GetUtcNow());
        db.Comentarios.Add(comment);
        await db.SaveChangesAsync(cancellationToken);
        return await LoadCommentAsync(comment.Id, includeModeration: false, cancellationToken);
    }

    public async Task<PagedResponse<ComentarioResponse>> ListCommentsAsync(
        Guid demandaId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        ValidatePage(page, pageSize);
        await EnsureDemandVisibleAsync(demandaId, cancellationToken);
        var query = db.Comentarios.AsNoTracking()
            .Where(x => x.DemandaId == demandaId && !x.Removido && !x.Oculto);
        var total = await query.CountAsync(cancellationToken);
        var comments = await query.OrderByDescending(x => x.CriadoEm).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new ComentarioResponse(
                x.Id, x.DemandaId, x.AutorId,
                db.Users.Where(u => u.Id == x.AutorId).Select(u => u.Alias).Single(),
                x.Texto, x.CriadoEm, false, null))
            .ToListAsync(cancellationToken);
        return new PagedResponse<ComentarioResponse>(comments, page, pageSize, total);
    }

    public async Task DeleteCommentAsync(Guid comentarioId, CancellationToken cancellationToken)
    {
        var comment = await db.Comentarios.SingleOrDefaultAsync(x => x.Id == comentarioId, cancellationToken)
            ?? throw AjaProblemException.NotFound("Comentário não encontrado.");
        if (comment.AutorId != currentUser.Id)
            throw AjaProblemException.Forbidden("Somente o autor pode remover este comentário.");
        comment.Remover(currentUser.Id, clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ComentarioResponse> ModerateCommentAsync(
        Guid comentarioId,
        ModerarComentarioRequest request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAdmin) throw AjaProblemException.Forbidden();
        var comment = await db.Comentarios.SingleOrDefaultAsync(x => x.Id == comentarioId, cancellationToken)
            ?? throw AjaProblemException.NotFound("Comentário não encontrado.");
        comment.Ocultar(request.Motivo, currentUser.Id, clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Admin {AdminId} ocultou comentário {ComentarioId}; utc={Utc}",
            currentUser.Id, comentarioId, clock.GetUtcNow());
        return await LoadCommentAsync(comentarioId, includeModeration: true, cancellationToken);
    }

    private async Task EnsureDemandVisibleAsync(Guid id, CancellationToken cancellationToken)
    {
        var query = db.Demandas.AsNoTracking().Where(x => x.Id == id && !x.Removida);
        if (!currentUser.IsAdmin)
            query = query.Where(x => x.Visibilidade == VisibilidadeDemanda.Publica ||
                (x.Visibilidade == VisibilidadeDemanda.RestritaAoAutorEAdmin && x.AutorId == currentUser.Id));
        if (!await query.AnyAsync(cancellationToken))
            throw AjaProblemException.NotFound("Demanda não encontrada.");
    }

    private async Task<ComentarioResponse> LoadCommentAsync(
        Guid id,
        bool includeModeration,
        CancellationToken cancellationToken) =>
        await db.Comentarios.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new ComentarioResponse(
                x.Id, x.DemandaId, x.AutorId,
                db.Users.Where(u => u.Id == x.AutorId).Select(u => u.Alias).Single(),
                x.Texto, x.CriadoEm, includeModeration && x.Oculto,
                includeModeration ? x.MotivoModeracao : null))
            .SingleAsync(cancellationToken);

    private static void ValidatePage(int page, int pageSize)
    {
        if (page < 1 || pageSize is < 1 or > 100)
            throw AjaProblemException.BadRequest("page deve ser >= 1 e pageSize deve estar entre 1 e 100.");
    }
}
