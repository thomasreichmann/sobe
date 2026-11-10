using Microsoft.EntityFrameworkCore;
using Sobe.Api.Data;
using Sobe.Api.Models;

namespace Sobe.Api.Repositories;

public interface IEquipamentoRepository
{
    Task<List<Equipamento>> ListarAsync();
    Task<Equipamento?> BuscarAsync(int id);
    void Adicionar(Equipamento equipamento);
    void Adicionar(Sensor sensor);
    void Remover(Equipamento equipamento);
    Task SalvarAsync();
}

public class EquipamentoRepository(SobeDbContext db) : IEquipamentoRepository
{
    public Task<List<Equipamento>> ListarAsync() =>
        db.Equipamentos.Include(q => q.Estacao).ThenInclude(e => e.Linha).ToListAsync();

    public Task<Equipamento?> BuscarAsync(int id) =>
        db.Equipamentos
            .Include(q => q.Estacao).ThenInclude(e => e.Linha)
            .Include(q => q.Alertas.Where(a => a.EncerradoEm == null))
            .FirstOrDefaultAsync(q => q.Id == id);

    public void Adicionar(Equipamento equipamento) => db.Equipamentos.Add(equipamento);

    public void Adicionar(Sensor sensor) => db.Sensores.Add(sensor);

    public void Remover(Equipamento equipamento) => db.Equipamentos.Remove(equipamento);

    public Task SalvarAsync() => db.SaveChangesAsync();
}
