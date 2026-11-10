using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sobe.Api.Dtos;
using Sobe.Api.Models;
using Sobe.Api.Security;
using Sobe.Api.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Sobe.Api.Controllers;

[ApiController]
[Route("api/relatos")]
[Authorize]
[Produces("application/json")]
public class RelatosController(RelatoService relatos) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "Lista relatos", Description = "O cidadão vê os próprios; o administrador vê todos.")]
    public Task<List<RelatoResponse>> Listar([FromQuery] StatusRelato? status) =>
        relatos.ListarAsync(User.Id(), User.EhAdmin(), status);

    [HttpGet("{id:int}")]
    [SwaggerOperation(Summary = "Detalhe de um relato")]
    [ProducesResponseType<RelatoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<RelatoResponse> Buscar(int id) => relatos.BuscarAsync(id, User.Id(), User.EhAdmin());

    [HttpPost]
    [SwaggerOperation(Summary = "Registra um problema visto num equipamento")]
    [ProducesResponseType<RelatoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RelatoResponse>> Criar(NovoRelatoRequest pedido)
    {
        var criado = await relatos.CriarAsync(User.Id(), pedido);
        return CreatedAtAction(nameof(Buscar), new { id = criado.Id }, criado);
    }

    [HttpPut("{id:int}")]
    [SwaggerOperation(Summary = "Corrige a descrição de um relato próprio que ainda está aberto")]
    [ProducesResponseType<RelatoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<RelatoResponse> Editar(int id, EditarRelatoRequest pedido) => relatos.EditarAsync(id, User.Id(), pedido);

    [HttpPut("{id:int}/status")]
    [Authorize(Roles = Perfis.Admin)]
    [SwaggerOperation(Summary = "Confirma, resolve ou descarta um relato (administrador)")]
    [ProducesResponseType<RelatoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<RelatoResponse> MudarStatus(int id, StatusRelatoRequest pedido) =>
        relatos.MudarStatusAsync(id, User.Id(), pedido.Status!.Value);

    [HttpDelete("{id:int}")]
    [SwaggerOperation(Summary = "Apaga um relato", Description = "O autor apaga enquanto está aberto; o administrador, sempre.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Remover(int id)
    {
        await relatos.RemoverAsync(id, User.Id(), User.EhAdmin());
        return NoContent();
    }
}
