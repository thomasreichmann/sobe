using Microsoft.EntityFrameworkCore;
using Sobe.Api.Data;
using Sobe.Api.Models;

namespace Sobe.Api.Repositories;

public interface ISensorRepository
{
    Task<List<Sensor>> ListarAsync();
    Task<Sensor?> BuscarAsync(int id);
    Task<Sensor?> BuscarPorCodigoAsync(string codigo);
    Task<Leitura?> UltimaLeituraAsync(int sensorId);
    void Adicionar(Leitura leitura);
    Task SalvarAsync();
}

public class SensorRepository(SobeDbContext db) : ISensorRepository
{
    public Task<List<Sensor>> ListarAsync() =>
        db.Sensores
            .Include(s => s.Equipamento).ThenInclude(q => q.Estacao).ThenInclude(e => e.Linha)
            .OrderBy(s => s.Codigo)
            .ToListAsync();

    public Task<Sensor?> BuscarAsync(int id) =>
        db.Sensores
            .Include(s => s.Equipamento).ThenInclude(q => q.Estacao).ThenInclude(e => e.Linha)
            .FirstOrDefaultAsync(s => s.Id == id);

    public Task<Sensor?> BuscarPorCodigoAsync(string codigo) =>
        db.Sensores
            .Include(s => s.Equipamento).ThenInclude(q => q.Sensores)
            .FirstOrDefaultAsync(s => s.Codigo == codigo);

    public Task<Leitura?> UltimaLeituraAsync(int sensorId) =>
        db.Leituras
            .Where(l => l.SensorId == sensorId)
            .OrderByDescending(l => l.RegistradaEm).ThenByDescending(l => l.Id)
            .FirstOrDefaultAsync();

    public void Adicionar(Leitura leitura) => db.Leituras.Add(leitura);

    public Task SalvarAsync() => db.SaveChangesAsync();
}
