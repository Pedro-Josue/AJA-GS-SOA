using Aja.Api.Modules.Auth.Domain;
using Aja.Api.Modules.Demandas.Application;
using Aja.Api.Modules.Demandas.Domain;
using Aja.Api.Shared.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aja.Api.Modules.Demandas.Api;

[ApiController]
[Route("api/v1/demandas")]
public sealed class DemandasController(IDemandaService service) : ControllerBase
{
    /// <summary>Abre uma demanda com descrição e local; o link de evidência é opcional.</summary>
    [HttpPost]
    [ProducesResponseType<DemandaDetalheResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<DemandaDetalheResponse>> Create(
        CriarDemandaRequest request,
        CancellationToken cancellationToken)
    {
        var created = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Demanda.Id }, created);
    }

    /// <summary>Lista demandas visíveis com filtros combináveis e paginação.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResponse<DemandaResumoResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<DemandaResumoResponse>>> List(
        [FromQuery] SituacaoDemanda? situacao,
        [FromQuery] CategoriaDemanda? categoria,
        [FromQuery] string? bairro,
        [FromQuery] bool minhas = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await service.ListAsync(situacao, categoria, bairro, minhas, page, pageSize, cancellationToken));

    /// <summary>Retorna o resumo atual, totais de participação e histórico de situação.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<DemandaDetalheResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DemandaDetalheResponse>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.GetAsync(id, cancellationToken));

    [HttpPut("{id:guid}")]
    [ProducesResponseType<DemandaDetalheResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DemandaDetalheResponse>> Update(
        Guid id,
        EditarDemandaRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Permite ao Admin classificar e/ou mudar a situação com histórico automático.</summary>
    [Authorize(Roles = Perfis.Admin)]
    [HttpPatch("{id:guid}")]
    [ProducesResponseType<DemandaDetalheResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DemandaDetalheResponse>> UpdateAdministration(
        Guid id,
        AtualizarDemandaAdminRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.UpdateAdministrationAsync(id, request, cancellationToken));
}
