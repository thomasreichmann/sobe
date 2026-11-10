using Microsoft.EntityFrameworkCore;
using Sobe.Api.Data;
using Sobe.Api.Models;

namespace Sobe.Api.Repositories;

public interface IRelatoRepository
{
    Task<List<Relato>> ListarAsync(int? usuarioId, StatusRelato? status);
    Task<Relato?> BuscarAsync(int id);
    Task<int> ContarAbertosAsync();
    void Adicionar(Relato relato);
    void Remover(Relato relato);
    Task SalvarAsync();
}

public class RelatoRepository(SobeDbContext db) : IRelatoRepository
{
    private const int MaximoPorConsulta = 200;

    private IQueryable<Relato> Completos =>
        db.Relatos
            .Include(r => r.Usuario)
            .Include(r => r.Equipamento).ThenInclude(q => q.Estacao).ThenInclude(e => e.Linha);

    public Task<List<Relato>> ListarAsync(int? usuarioId, StatusRelato? status) =>
        Completos
            .Where(r => usuarioId == null || r.UsuarioId == usuarioId)
            .Where(r => status == null || r.Status == status)
            .OrderByDescending(r => r.CriadoEm)
            .Take(MaximoPorConsulta)
            .ToListAsync();

    public Task<Relato?> BuscarAsync(int id) => Completos.FirstOrDefaultAsync(r => r.Id == id);

    public Task<int> ContarAbertosAsync() => db.Relatos.CountAsync(r => r.Status == StatusRelato.Aberto);

    public void Adicionar(Relato relato) => db.Relatos.Add(relato);

    public void Remover(Relato relato) => db.Relatos.Remove(relato);

    public Task SalvarAsync() => db.SaveChangesAsync();
}
