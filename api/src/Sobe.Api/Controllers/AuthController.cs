using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Sobe.Api.Dtos;
using Sobe.Api.Security;
using Sobe.Api.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Sobe.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(AuthService auth) : ControllerBase
{
    [HttpPost("registrar")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [SwaggerOperation(Summary = "Cria uma conta de cidadão e devolve o token de acesso")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LoginResponse>> Registrar(RegistrarRequest pedido)
    {
        var resposta = await auth.RegistrarAsync(pedido);
        return CreatedAtAction(nameof(Eu), resposta);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [SwaggerOperation(Summary = "Confere e-mail e senha e devolve o token de acesso")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public Task<LoginResponse> Entrar(LoginRequest pedido) => auth.EntrarAsync(pedido);

    [HttpGet("eu")]
    [Authorize]
    [SwaggerOperation(Summary = "Dados de quem está autenticado")]
    public Task<UsuarioResponse> Eu() => auth.BuscarAsync(User.Id());
}
