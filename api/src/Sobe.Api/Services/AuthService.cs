using Sobe.Api.Dtos;
using Sobe.Api.Infra;
using Sobe.Api.Models;
using Sobe.Api.Repositories;
using Sobe.Api.Security;

namespace Sobe.Api.Services;

public class AuthService(IUsuarioRepository usuarios, TokenService tokens, TimeProvider relogio)
{
    public async Task<LoginResponse> RegistrarAsync(RegistrarRequest pedido)
    {
        var email = pedido.Email.Trim().ToLowerInvariant();
        if (await usuarios.BuscarPorEmailAsync(email) is not null) throw new Conflito("Já existe uma conta com este e-mail.");

        var usuario = new Usuario
        {
            Nome = pedido.Nome.Trim(),
            Email = email,
            SenhaHash = SenhaHasher.Gerar(pedido.Senha),
            Perfil = Perfil.Cidadao,
            CriadoEm = relogio.GetUtcNow().UtcDateTime,
        };
        usuarios.Adicionar(usuario);
        await usuarios.SalvarAsync();
        return tokens.Emitir(usuario);
    }

    public async Task<LoginResponse> EntrarAsync(LoginRequest pedido)
    {
        var usuario = await usuarios.BuscarPorEmailAsync(pedido.Email.Trim().ToLowerInvariant());
        var senhaConfere = SenhaHasher.Conferir(pedido.Senha, usuario?.SenhaHash ?? SenhaHasher.HashDeDescarte);
        if (usuario is null || !senhaConfere) throw new CredenciaisInvalidas();
        return tokens.Emitir(usuario);
    }

    public async Task<UsuarioResponse> BuscarAsync(int id)
    {
        var usuario = await usuarios.BuscarAsync(id) ?? throw new NaoEncontrado("Usuário não encontrado.");
        return usuario.ParaResposta();
    }
}
