using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sobe.Api.Dtos;
using Sobe.Api.Security;
using Sobe.Api.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Sobe.Api.Controllers;

[ApiController]
[Route("api/favoritos")]
[Authorize]
[Produces("application/json")]
public class FavoritosController(FavoritoService favoritos, EstacaoService estacoes) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "Estações que o usuário acompanha")]
    public Task<List<EstacaoResumoResponse>> Listar() =>
        estacoes.ListarAsync(linhaId: null, busca: null, somenteFavoritas: true, User.Id());

    [HttpPut("{estacaoId:int}")]
    [SwaggerOperation(Summary = "Passa a acompanhar uma estação")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Adicionar(int estacaoId)
    {
        await favoritos.AdicionarAsync(User.Id(), estacaoId);
        return NoContent();
    }

    [HttpDelete("{estacaoId:int}")]
    [SwaggerOperation(Summary = "Deixa de acompanhar uma estação")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Remover(int estacaoId)
    {
        await favoritos.RemoverAsync(User.Id(), estacaoId);
        return NoContent();
    }
}
