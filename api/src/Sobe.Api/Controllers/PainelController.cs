using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sobe.Api.Dtos;
using Sobe.Api.Security;
using Sobe.Api.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Sobe.Api.Controllers;

[ApiController]
[Route("api/painel")]
[Authorize(Roles = Perfis.Admin)]
[Produces("application/json")]
public class PainelController(PainelService painel) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "Resumo da operação nos últimos 30 dias (administrador)")]
    public Task<PainelResponse> Resumir() => painel.ResumirAsync();
}
