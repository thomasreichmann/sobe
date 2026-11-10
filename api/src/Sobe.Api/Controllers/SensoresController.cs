using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sobe.Api.Dtos;
using Sobe.Api.Security;
using Sobe.Api.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Sobe.Api.Controllers;

[ApiController]
[Produces("application/json")]
public class SensoresController(SensorService sensores) : ControllerBase
{
    private const string TokenOuChave = JwtBearerDefaults.AuthenticationScheme + "," + DispositivoAuthHandler.Esquema;

    [HttpGet("api/sensores")]
    [Authorize(AuthenticationSchemes = TokenOuChave, Roles = Perfis.Admin + "," + Perfis.Dispositivo)]
    [SwaggerOperation(Summary = "Lista os sensores (administrador ou chave de dispositivo)")]
    public Task<List<SensorResponse>> Listar() => sensores.ListarAsync();

    [HttpPut("api/sensores/{id:int}")]
    [Authorize(Roles = Perfis.Admin)]
    [SwaggerOperation(Summary = "Ajusta o limite de um sensor ou o desativa (administrador)")]
    [ProducesResponseType<SensorResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<SensorResponse> Atualizar(int id, SensorRequest pedido) => sensores.AtualizarAsync(id, pedido);

    [HttpPost("api/leituras")]
    [Authorize(AuthenticationSchemes = DispositivoAuthHandler.Esquema, Roles = Perfis.Dispositivo)]
    [SwaggerOperation(Summary = "Recebe a leitura de um sensor (chave de dispositivo)",
        Description = "Acima do limite, o equipamento passa a PARADO e um alerta é aberto. Quando todos os sensores do equipamento voltam ao normal, o alerta é encerrado.")]
    [ProducesResponseType<LeituraResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LeituraResponse>> RegistrarLeitura(LeituraRequest pedido)
    {
        var leitura = await sensores.RegistrarLeituraAsync(pedido);
        return StatusCode(StatusCodes.Status201Created, leitura);
    }
}
