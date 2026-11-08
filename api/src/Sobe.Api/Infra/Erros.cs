using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Sobe.Api.Infra;

public abstract class ErroDeNegocio(string mensagem, int status, string titulo) : Exception(mensagem)
{
    public int Status => status;
    public string Titulo => titulo;
}

public class NaoEncontrado(string mensagem) : ErroDeNegocio(mensagem, StatusCodes.Status404NotFound, "Não encontrado");

public class Conflito(string mensagem) : ErroDeNegocio(mensagem, StatusCodes.Status409Conflict, "Conflito");

public class RegraViolada(string mensagem) : ErroDeNegocio(mensagem, StatusCodes.Status422UnprocessableEntity, "Operação não permitida");

public class AcessoNegado(string mensagem) : ErroDeNegocio(mensagem, StatusCodes.Status403Forbidden, "Acesso negado");

public class CredenciaisInvalidas() : ErroDeNegocio("E-mail ou senha incorretos.", StatusCodes.Status401Unauthorized, "Não autenticado");

public class FiltroDeErros(ProblemDetailsFactory fabrica) : IExceptionFilter
{
    public void OnException(ExceptionContext contexto)
    {
        if (contexto.Exception is not ErroDeNegocio erro) return;

        var problema = fabrica.CreateProblemDetails(contexto.HttpContext, erro.Status, erro.Titulo, detail: erro.Message);
        contexto.Result = new ObjectResult(problema) { StatusCode = erro.Status };
        contexto.ExceptionHandled = true;
    }
}
