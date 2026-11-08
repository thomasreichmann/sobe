using System.Security.Claims;

namespace Sobe.Api.Security;

public static class UsuarioAtual
{
    public static int? IdOuNulo(this ClaimsPrincipal principal) =>
        int.TryParse(principal.FindFirst("sub")?.Value, out var id) ? id : null;

    public static int Id(this ClaimsPrincipal principal) =>
        principal.IdOuNulo() ?? throw new InvalidOperationException("Token sem o identificador do usuário.");

    public static bool EhAdmin(this ClaimsPrincipal principal) => principal.IsInRole(Perfis.Admin);
}
