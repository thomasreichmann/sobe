using Microsoft.EntityFrameworkCore;
using Sobe.Api.Data;
using Sobe.Api.Models;

namespace Sobe.Api.Repositories;

public interface IFavoritoRepository
{
    Task<HashSet<int>> ListarEstacoesAsync(int usuarioId);
    Task<Favorito?> BuscarAsync(int usuarioId, int estacaoId);
    void Adicionar(Favorito favorito);
    void Remover(Favorito favorito);
    Task SalvarAsync();
}

public class FavoritoRepository(SobeDbContext db) : IFavoritoRepository
{
    public async Task<HashSet<int>> ListarEstacoesAsync(int usuarioId) =>
        (await db.Favoritos.Where(f => f.UsuarioId == usuarioId).Select(f => f.EstacaoId).ToListAsync()).ToHashSet();

    public Task<Favorito?> BuscarAsync(int usuarioId, int estacaoId) =>
        db.Favoritos.FirstOrDefaultAsync(f => f.UsuarioId == usuarioId && f.EstacaoId == estacaoId);

    public void Adicionar(Favorito favorito) => db.Favoritos.Add(favorito);

    public void Remover(Favorito favorito) => db.Favoritos.Remove(favorito);

    public Task SalvarAsync() => db.SaveChangesAsync();
}
