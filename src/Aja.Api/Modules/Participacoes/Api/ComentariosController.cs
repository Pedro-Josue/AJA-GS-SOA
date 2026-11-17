using Aja.Api.Modules.Auth.Domain;
using Aja.Api.Modules.Participacoes.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aja.Api.Modules.Participacoes.Api;

[ApiController]
[Route("api/v1/comentarios")]
public sealed class ComentariosController(IParticipacaoService service) : ControllerBase
{
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await service.DeleteCommentAsync(id, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = Perfis.Admin)]
    [HttpPatch("{id:guid}/moderacao")]
    [ProducesResponseType<ComentarioResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ComentarioResponse>> Moderate(
        Guid id,
        ModerarComentarioRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.ModerateCommentAsync(id, request, cancellationToken));
}
