using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sobe.Api.Dtos;
using Sobe.Api.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Sobe.Api.Controllers;

[ApiController]
[Route("api/linhas")]
[Produces("application/json")]
public class LinhasController(EstacaoService estacoes) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Lista as linhas")]
    public Task<List<LinhaResponse>> Listar() => estacoes.ListarLinhasAsync();
}
