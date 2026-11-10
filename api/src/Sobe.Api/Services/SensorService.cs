using Sobe.Api.Dtos;
using Sobe.Api.Infra;
using Sobe.Api.Models;
using Sobe.Api.Repositories;

namespace Sobe.Api.Services;

public class SensorService(ISensorRepository sensores, IAlertaRepository alertas, TimeProvider relogio)
{
    public async Task<List<SensorResponse>> ListarAsync() =>
        (await sensores.ListarAsync()).Select(s => s.ParaResposta()).ToList();

    public async Task<SensorResponse> AtualizarAsync(int id, SensorRequest pedido)
    {
        var sensor = await sensores.BuscarAsync(id) ?? throw new NaoEncontrado("Sensor não encontrado.");
        sensor.Limite = pedido.Limite;
        sensor.Ativo = pedido.Ativo;
        await sensores.SalvarAsync();
        return sensor.ParaResposta();
    }

    public async Task<LeituraResponse> RegistrarLeituraAsync(LeituraRequest pedido)
    {
        var codigo = pedido.Sensor.Trim().ToUpperInvariant();
        var sensor = await sensores.BuscarPorCodigoAsync(codigo) ?? throw new NaoEncontrado($"Sensor {codigo} não cadastrado.");
        if (!sensor.Ativo) throw new RegraViolada($"O sensor {codigo} está desativado.");

        var agora = relogio.GetUtcNow().UtcDateTime;
        var equipamento = sensor.Equipamento;
        var alertaAberto = await alertas.BuscarAbertoAsync(equipamento.Id);
        var acimaDoLimite = sensor.Ultrapassa(pedido.Valor);

        var leitura = new Leitura { SensorId = sensor.Id, Valor = pedido.Valor, RegistradaEm = agora };
        sensores.Adicionar(leitura);

        if (equipamento.Status != StatusEquipamento.Manutencao)
        {
            if (acimaDoLimite)
            {
                alertaAberto ??= AbrirAlerta(sensor, pedido.Valor, agora);
                equipamento.MudarStatus(StatusEquipamento.Parado, agora);
            }
            else if (alertaAberto?.SensorId is not null && await OutrosSensoresNormaisAsync(sensor))
            {
                alertaAberto.EncerradoEm = agora;
                alertaAberto = null;
                equipamento.MudarStatus(StatusEquipamento.Operando, agora);
            }
        }

        await sensores.SalvarAsync();
        return new LeituraResponse(
            leitura.Id, sensor.Codigo, leitura.Valor, sensor.Limite, acimaDoLimite, agora, equipamento.Status, alertaAberto?.Id);
    }

    private Alerta AbrirAlerta(Sensor sensor, double valor, DateTime agora)
    {
        var alerta = new Alerta
        {
            EquipamentoId = sensor.EquipamentoId,
            SensorId = sensor.Id,
            Motivo = sensor.DescreverParada(valor),
            AbertoEm = agora,
        };
        alertas.Adicionar(alerta);
        return alerta;
    }

    private async Task<bool> OutrosSensoresNormaisAsync(Sensor atual)
    {
        foreach (var outro in atual.Equipamento.Sensores.Where(s => s.Id != atual.Id && s.Ativo))
        {
            var ultima = await sensores.UltimaLeituraAsync(outro.Id);
            if (ultima is not null && outro.Ultrapassa(ultima.Valor)) return false;
        }
        return true;
    }
}
