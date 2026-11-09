using Microsoft.EntityFrameworkCore;
using Sobe.Api.Data;
using Sobe.Api.Models;

namespace Sobe.Api.Repositories;

public interface IEstacaoRepository
{
    Task<List<Linha>> ListarLinhasAsync();
    Task<Linha?> BuscarLinhaAsync(int id);
    Task<List<Estacao>> ListarAsync(int? linhaId);
    Task<Estacao?> BuscarAsync(int id);
    Task<bool> NomeEmUsoAsync(int linhaId, string nome, int? ignorarId);
    Task<bool> OrdemEmUsoAsync(int linhaId, int ordem, int? ignorarId);
    void Adicionar(Estacao estacao);
    void Remover(Estacao estacao);
    Task SalvarAsync();
}

public class EstacaoRepository(SobeDbContext db) : IEstacaoRepository
{
    public Task<List<Linha>> ListarLinhasAsync() => db.Linhas.OrderBy(l => l.Numero).ToListAsync();

    public Task<Linha?> BuscarLinhaAsync(int id) => db.Linhas.FirstOrDefaultAsync(l => l.Id == id);

    public Task<List<Estacao>> ListarAsync(int? linhaId) =>
        db.Estacoes
            .Include(e => e.Linha)
            .Include(e => e.Equipamentos)
            .Where(e => linhaId == null || e.LinhaId == linhaId)
            .OrderBy(e => e.Linha.Numero).ThenBy(e => e.Ordem)
            .ToListAsync();

    public Task<Estacao?> BuscarAsync(int id) =>
        db.Estacoes
            .Include(e => e.Linha)
            .Include(e => e.Equipamentos.OrderBy(q => q.Id))
                .ThenInclude(q => q.Alertas.Where(a => a.EncerradoEm == null))
            .FirstOrDefaultAsync(e => e.Id == id);

    public Task<bool> NomeEmUsoAsync(int linhaId, string nome, int? ignorarId) =>
        db.Estacoes.AnyAsync(e => e.LinhaId == linhaId && e.Nome == nome && e.Id != (ignorarId ?? 0));

    public Task<bool> OrdemEmUsoAsync(int linhaId, int ordem, int? ignorarId) =>
        db.Estacoes.AnyAsync(e => e.LinhaId == linhaId && e.Ordem == ordem && e.Id != (ignorarId ?? 0));

    public void Adicionar(Estacao estacao) => db.Estacoes.Add(estacao);

    public void Remover(Estacao estacao) => db.Estacoes.Remove(estacao);

    public Task SalvarAsync() => db.SaveChangesAsync();
}
