using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sobe.Api.Dtos;
using Sobe.Api.Security;
using Sobe.Api.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Sobe.Api.Controllers;

[ApiController]
[Route("api/equipamentos")]
[Produces("application/json")]
public class EquipamentosController(EquipamentoService equipamentos) : ControllerBase
{
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Situação de um equipamento")]
    [ProducesResponseType<EquipamentoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<EquipamentoResponse> Buscar(int id) => equipamentos.BuscarAsync(id);

    [HttpPut("{id:int}")]
    [Authorize(Roles = Perfis.Admin)]
    [SwaggerOperation(Summary = "Altera a localização e a situação de um equipamento (administrador)",
        Description = "Marcar como PARADO abre um alerta; voltar para OPERANDO ou MANUTENCAO encerra o alerta aberto.")]
    [ProducesResponseType<EquipamentoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<EquipamentoResponse> Atualizar(int id, EquipamentoRequest pedido) => equipamentos.AtualizarAsync(id, pedido);

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Perfis.Admin)]
    [SwaggerOperation(Summary = "Remove um equipamento (administrador)")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remover(int id)
    {
        await equipamentos.RemoverAsync(id);
        return NoContent();
    }
}
