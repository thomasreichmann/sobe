using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Sobe.Api.Security;

public class DispositivoAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptions<DispositivoOptions> dispositivo) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string Esquema = "Dispositivo";
    public const string Cabecalho = "X-Chave-Dispositivo";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(Cabecalho, out var valores)) return Task.FromResult(AuthenticateResult.NoResult());

        var recebida = Encoding.UTF8.GetBytes(valores.ToString());
        var esperada = Encoding.UTF8.GetBytes(dispositivo.Value.Chave);
        if (!CryptographicOperations.FixedTimeEquals(recebida, esperada))
            return Task.FromResult(AuthenticateResult.Fail("Chave de dispositivo inválida."));

        var identidade = new ClaimsIdentity([new Claim("role", Perfis.Dispositivo)], Esquema, "name", "role");
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identidade), Esquema)));
    }
}
