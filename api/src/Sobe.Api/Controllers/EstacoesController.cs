using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sobe.Api.Dtos;
using Sobe.Api.Security;
using Sobe.Api.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Sobe.Api.Controllers;

[ApiController]
[Route("api/estacoes")]
[Produces("application/json")]
public class EstacoesController(EstacaoService estacoes, EquipamentoService equipamentos) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Lista as estações com a situação dos equipamentos",
        Description = "A busca ignora acentos. Com token, cada estação indica se é favorita de quem consulta.")]
    public Task<List<EstacaoResumoResponse>> Listar(
        [FromQuery] int? linhaId, [FromQuery] string? busca, [FromQuery] bool somenteFavoritas = false) =>
        estacoes.ListarAsync(linhaId, busca, somenteFavoritas, User.IdOuNulo());

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Detalhe de uma estação",
        Description = "Traz os equipamentos, o alerta aberto de cada um e, se a estação não estiver acessível, a alternativa mais próxima na mesma linha.")]
    [ProducesResponseType<EstacaoDetalheResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<EstacaoDetalheResponse> Buscar(int id) => estacoes.BuscarAsync(id, User.IdOuNulo());

    [HttpPost]
    [Authorize(Roles = Perfis.Admin)]
    [SwaggerOperation(Summary = "Cadastra uma estação (administrador)")]
    [ProducesResponseType<EstacaoDetalheResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EstacaoDetalheResponse>> Criar(EstacaoRequest pedido)
    {
        var criada = await estacoes.CriarAsync(pedido);
        return CreatedAtAction(nameof(Buscar), new { id = criada.Id }, criada);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Perfis.Admin)]
    [SwaggerOperation(Summary = "Altera uma estação (administrador)")]
    [ProducesResponseType<EstacaoDetalheResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<EstacaoDetalheResponse> Atualizar(int id, EstacaoRequest pedido) => estacoes.AtualizarAsync(id, pedido);

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Perfis.Admin)]
    [SwaggerOperation(Summary = "Remove uma estação e os equipamentos dela (administrador)")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remover(int id)
    {
        await estacoes.RemoverAsync(id);
        return NoContent();
    }

    [HttpPost("{id:int}/equipamentos")]
    [Authorize(Roles = Perfis.Admin)]
    [SwaggerOperation(Summary = "Cadastra um equipamento na estação, já com os sensores (administrador)")]
    [ProducesResponseType<EquipamentoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EquipamentoResponse>> CriarEquipamento(int id, NovoEquipamentoRequest pedido)
    {
        var criado = await equipamentos.CriarAsync(id, pedido);
        return CreatedAtAction(nameof(EquipamentosController.Buscar), "Equipamentos", new { id = criado.Id }, criado);
    }
}
