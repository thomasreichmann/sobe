using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Sobe.Api.Dtos;
using Sobe.Api.Infra;
using Sobe.Api.Models;

namespace Sobe.Api.Security;

public class TokenService(IOptions<JwtOptions> opcoes, TimeProvider relogio)
{
    public LoginResponse Emitir(Usuario usuario)
    {
        var jwt = opcoes.Value;
        var agora = relogio.GetUtcNow().UtcDateTime;
        var expiraEm = agora.AddMinutes(jwt.ValidadeMinutos);

        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = jwt.Emissor,
            Audience = jwt.Emissor,
            Subject = new ClaimsIdentity(
            [
                new Claim("sub", usuario.Id.ToString()),
                new Claim("name", usuario.Nome),
                new Claim("role", usuario.Perfil.ParaTexto()),
            ]),
            IssuedAt = agora,
            NotBefore = agora,
            Expires = expiraEm,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Chave)), SecurityAlgorithms.HmacSha256),
        });

        return new LoginResponse(token, expiraEm, usuario.ParaResposta());
    }
}
