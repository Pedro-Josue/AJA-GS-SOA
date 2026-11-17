using Aja.Api.Modules.Participacoes.Application;
using Aja.Api.Shared.Configuration;
using Microsoft.AspNetCore.Mvc;

namespace Aja.Api.Modules.Participacoes.Api;

[ApiController]
[Route("api/v1/demandas/{demandaId:guid}")]
public sealed class DemandasParticipacoesController(IParticipacaoService service) : ControllerBase
{
    [HttpPut("apoio")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Support(Guid demandaId, CancellationToken cancellationToken)
    {
        await service.AddSupportAsync(demandaId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("apoio")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveSupport(Guid demandaId, CancellationToken cancellationToken)
    {
        await service.RemoveSupportAsync(demandaId, cancellationToken);
        return NoContent();
    }

    [HttpPost("comentarios")]
    [ProducesResponseType<ComentarioResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ComentarioResponse>> CreateComment(
        Guid demandaId,
        CriarComentarioRequest request,
        CancellationToken cancellationToken)
    {
        var created = await service.CreateCommentAsync(demandaId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpGet("comentarios")]
    [ProducesResponseType<PagedResponse<ComentarioResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ComentarioResponse>>> ListComments(
        Guid demandaId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await service.ListCommentsAsync(demandaId, page, pageSize, cancellationToken));
}
