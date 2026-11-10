using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sobe.Api.Dtos;
using Sobe.Api.Security;
using Sobe.Api.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Sobe.Api.Controllers;

[ApiController]
[Route("api/alertas")]
[Authorize]
[Produces("application/json")]
public class AlertasController(AlertaService alertas) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "Paradas detectadas, das mais recentes para as mais antigas",
        Description = "Por padrão traz só as paradas em aberto. Com somenteFavoritas, só as das estações que o usuário acompanha.")]
    public Task<List<AlertaResponse>> Listar([FromQuery] bool somenteAbertos = true, [FromQuery] bool somenteFavoritas = false) =>
        alertas.ListarAsync(somenteAbertos, somenteFavoritas, User.Id());
}
