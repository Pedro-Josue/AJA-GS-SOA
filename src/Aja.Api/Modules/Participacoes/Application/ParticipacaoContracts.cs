using System.ComponentModel.DataAnnotations;
using Aja.Api.Shared.Configuration;

namespace Aja.Api.Modules.Participacoes.Application;

public sealed record CriarComentarioRequest(
    [param: Required, StringLength(3000, MinimumLength = 3)] string Texto);

public sealed record ModerarComentarioRequest(
    [param: Required, StringLength(2000, MinimumLength = 5)] string Motivo);

public sealed record ComentarioResponse(
    Guid Id,
    Guid DemandaId,
    string AutorId,
    string AutorAlias,
    string Texto,
    DateTimeOffset CriadoEm,
    bool Oculto = false,
    string? MotivoModeracao = null);

public interface IParticipacaoService
{
    Task AddSupportAsync(Guid demandaId, CancellationToken cancellationToken);
    Task RemoveSupportAsync(Guid demandaId, CancellationToken cancellationToken);
    Task<ComentarioResponse> CreateCommentAsync(Guid demandaId, CriarComentarioRequest request, CancellationToken cancellationToken);
    Task<PagedResponse<ComentarioResponse>> ListCommentsAsync(Guid demandaId, int page, int pageSize, CancellationToken cancellationToken);
    Task DeleteCommentAsync(Guid comentarioId, CancellationToken cancellationToken);
    Task<ComentarioResponse> ModerateCommentAsync(Guid comentarioId, ModerarComentarioRequest request, CancellationToken cancellationToken);
}
