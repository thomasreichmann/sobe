using Sobe.Api.Dtos;
using Sobe.Api.Infra;
using Sobe.Api.Models;
using Sobe.Api.Repositories;

namespace Sobe.Api.Services;

public class EquipamentoService(IEquipamentoRepository equipamentos, IEstacaoRepository estacoes, TimeProvider relogio)
{
    public async Task<EquipamentoResponse> BuscarAsync(int id) => (await ObterAsync(id)).ParaResposta();

    public async Task<EquipamentoResponse> CriarAsync(int estacaoId, NovoEquipamentoRequest pedido)
    {
        if (await estacoes.BuscarAsync(estacaoId) is null) throw new NaoEncontrado("Estação não encontrada.");

        var equipamento = new Equipamento
        {
            EstacaoId = estacaoId,
            Tipo = pedido.Tipo!.Value,
            Localizacao = pedido.Localizacao.Trim(),
            StatusDesde = relogio.GetUtcNow().UtcDateTime,
        };
        equipamentos.Adicionar(equipamento);
        await equipamentos.SalvarAsync();

        equipamentos.Adicionar(Sensor.Padrao(TipoSensor.Movimento, equipamento.Id));
        if (equipamento.Tipo != TipoEquipamento.EscadaRolante) equipamentos.Adicionar(Sensor.Padrao(TipoSensor.Porta, equipamento.Id));
        await equipamentos.SalvarAsync();
        return equipamento.ParaResposta();
    }

    public async Task<EquipamentoResponse> AtualizarAsync(int id, EquipamentoRequest pedido)
    {
        var equipamento = await ObterAsync(id);
        var agora = relogio.GetUtcNow().UtcDateTime;
        var novoStatus = pedido.Status!.Value;
        var alertaAberto = equipamento.Alertas.FirstOrDefault(a => a.Aberto);

        if (novoStatus == StatusEquipamento.Parado && alertaAberto is null)
            equipamento.Alertas.Add(new Alerta { Motivo = "Parada registrada pela operadora", AbertoEm = agora });
        if (novoStatus != StatusEquipamento.Parado && alertaAberto is not null)
            alertaAberto.EncerradoEm = agora;

        equipamento.Localizacao = pedido.Localizacao.Trim();
        equipamento.MudarStatus(novoStatus, agora);
        await equipamentos.SalvarAsync();
        return equipamento.ParaResposta();
    }

    public async Task RemoverAsync(int id)
    {
        equipamentos.Remover(await ObterAsync(id));
        await equipamentos.SalvarAsync();
    }

    private async Task<Equipamento> ObterAsync(int id) =>
        await equipamentos.BuscarAsync(id) ?? throw new NaoEncontrado("Equipamento não encontrado.");
}
