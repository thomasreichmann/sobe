using Sobe.Api.Dtos;
using Sobe.Api.Models;
using Sobe.Api.Repositories;

namespace Sobe.Api.Services;

public class PainelService(IEquipamentoRepository equipamentos, IAlertaRepository alertas, IRelatoRepository relatos, TimeProvider relogio)
{
    private const int DiasDoPeriodo = 30;
    private const int MinutosDoPeriodo = DiasDoPeriodo * 24 * 60;

    public async Task<PainelResponse> ResumirAsync()
    {
        var agora = relogio.GetUtcNow().UtcDateTime;
        var todos = await equipamentos.ListarAsync();
        var paradas = await alertas.ListarDesdeAsync(agora.AddDays(-DiasDoPeriodo));

        var maisParados = paradas
            .GroupBy(a => a.EquipamentoId)
            .Select(g => new TempoParadoResponse(
                g.First().Equipamento.ParaLocal(), g.Count(), (int)Math.Round(g.Sum(a => a.MinutosParado(agora)))))
            .OrderByDescending(t => t.MinutosParado)
            .Take(10)
            .ToList();

        var disponibilidade = todos
            .Where(q => q.Tipo != TipoEquipamento.EscadaRolante)
            .GroupBy(q => q.Estacao.Linha)
            .Select(g => Disponibilidade(g.Key, g.ToList(), paradas, agora))
            .OrderBy(d => d.LinhaNumero)
            .ToList();

        return new PainelResponse(
            todos.Count,
            todos.Count(q => q.Status == StatusEquipamento.Operando),
            todos.Count(q => q.Status == StatusEquipamento.Parado),
            todos.Count(q => q.Status == StatusEquipamento.Manutencao),
            await alertas.ContarAbertosAsync(),
            await relatos.ContarAbertosAsync(),
            maisParados,
            disponibilidade);
    }

    private static DisponibilidadeResponse Disponibilidade(Linha linha, List<Equipamento> daLinha, List<Alerta> paradas, DateTime agora)
    {
        var ids = daLinha.Select(q => q.Id).ToHashSet();
        var minutosParados = paradas.Where(a => ids.Contains(a.EquipamentoId)).Sum(a => a.MinutosParado(agora));
        var percentual = 100 - minutosParados / (daLinha.Count * MinutosDoPeriodo) * 100;
        return new DisponibilidadeResponse(linha.Numero, linha.Nome, daLinha.Count, Math.Round(percentual, 2));
    }
}
