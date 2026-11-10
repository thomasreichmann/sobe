using Sobe.Api.Infra;
using Sobe.Api.Models;
using Sobe.Api.Repositories;

namespace Sobe.Api.Services;

public class FavoritoService(IFavoritoRepository favoritos, IEstacaoRepository estacoes, TimeProvider relogio)
{
    public async Task AdicionarAsync(int usuarioId, int estacaoId)
    {
        if (await estacoes.BuscarAsync(estacaoId) is null) throw new NaoEncontrado("Estação não encontrada.");
        if (await favoritos.BuscarAsync(usuarioId, estacaoId) is not null) return;

        favoritos.Adicionar(new Favorito { UsuarioId = usuarioId, EstacaoId = estacaoId, CriadoEm = relogio.GetUtcNow().UtcDateTime });
        await favoritos.SalvarAsync();
    }

    public async Task RemoverAsync(int usuarioId, int estacaoId)
    {
        var favorito = await favoritos.BuscarAsync(usuarioId, estacaoId);
        if (favorito is null) return;

        favoritos.Remover(favorito);
        await favoritos.SalvarAsync();
    }
}
