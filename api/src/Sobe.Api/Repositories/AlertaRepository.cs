using Microsoft.EntityFrameworkCore;
using Sobe.Api.Data;
using Sobe.Api.Models;

namespace Sobe.Api.Repositories;

public interface IAlertaRepository
{
    Task<Alerta?> BuscarAbertoAsync(int equipamentoId);
    Task<List<Alerta>> ListarAsync(bool somenteAbertos, int? favoritasDoUsuarioId);
    Task<List<Alerta>> ListarDesdeAsync(DateTime inicio);
    Task<int> ContarAbertosAsync();
    void Adicionar(Alerta alerta);
}

public class AlertaRepository(SobeDbContext db) : IAlertaRepository
{
    private const int MaximoPorConsulta = 100;

    public Task<Alerta?> BuscarAbertoAsync(int equipamentoId) =>
        db.Alertas.FirstOrDefaultAsync(a => a.EquipamentoId == equipamentoId && a.EncerradoEm == null);

    public Task<List<Alerta>> ListarAsync(bool somenteAbertos, int? favoritasDoUsuarioId) =>
        db.Alertas
            .Include(a => a.Equipamento).ThenInclude(q => q.Estacao).ThenInclude(e => e.Linha)
            .Where(a => !somenteAbertos || a.EncerradoEm == null)
            .Where(a => favoritasDoUsuarioId == null || db.Favoritos.Any(f =>
                f.UsuarioId == favoritasDoUsuarioId && f.EstacaoId == a.Equipamento.EstacaoId))
            .OrderByDescending(a => a.AbertoEm)
            .Take(MaximoPorConsulta)
            .ToListAsync();

    public Task<List<Alerta>> ListarDesdeAsync(DateTime inicio) =>
        db.Alertas
            .Include(a => a.Equipamento).ThenInclude(q => q.Estacao).ThenInclude(e => e.Linha)
            .Where(a => a.AbertoEm >= inicio)
            .ToListAsync();

    public Task<int> ContarAbertosAsync() => db.Alertas.CountAsync(a => a.EncerradoEm == null);

    public void Adicionar(Alerta alerta) => db.Alertas.Add(alerta);
}
