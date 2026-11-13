using System.Net.Http.Json;
using Sobe.Api.Dtos;
using Sobe.Api.Models;
using Sobe.Api.Tests.Infra;

namespace Sobe.Api.Tests;

public class PainelTests : TesteDeApi
{
    [Fact]
    public async Task Painel_conta_equipamentos_alertas_e_relatos_em_aberto()
    {
        var admin = await EntrarAsync(ApiFactory.Admin);
        var ana = await EntrarAsync(ApiFactory.Ana);
        await LeituraAsync(Cenario.MovimentoDaSe, 20);
        await admin.PutAsJsonAsync($"/api/equipamentos/{Cenario.ElevadorDaPaulista}", new EquipamentoRequest("Entre a rua e a plataforma", StatusEquipamento.Manutencao), Json);
        await ana.PostAsJsonAsync("/api/relatos", new NovoRelatoRequest(Cenario.ElevadorDaSe, "Elevador parado entre os andares."));

        var painel = await BuscarAsync<PainelResponse>(admin, "/api/painel");

        Assert.Equal(5, painel.Equipamentos);
        Assert.Equal(3, painel.Operando);
        Assert.Equal(1, painel.Parados);
        Assert.Equal(1, painel.EmManutencao);
        Assert.Equal(1, painel.AlertasAbertos);
        Assert.Equal(1, painel.RelatosAbertos);
    }

    [Fact]
    public async Task Tempo_parado_soma_as_paradas_dos_ultimos_30_dias_por_equipamento()
    {
        var admin = await EntrarAsync(ApiFactory.Admin);
        var agora = DateTime.UtcNow;
        Api.NoBanco(db =>
        {
            db.Alertas.AddRange(
                Parada(Cenario.ElevadorDaSe, agora.AddDays(-3), minutos: 90),
                Parada(Cenario.ElevadorDaSe, agora.AddDays(-2), minutos: 30),
                Parada(Cenario.ElevadorDaPaulista, agora.AddDays(-5), minutos: 200),
                Parada(Cenario.ElevadorDaLiberdade, agora.AddDays(-45), minutos: 5000));
            return db.SaveChanges();
        });

        var painel = await BuscarAsync<PainelResponse>(admin, "/api/painel");

        Assert.Equal(
            [("Paulista", 1, 200), ("Sé", 2, 120)],
            painel.MaisParados.Select(t => (t.Local.Estacao, t.Paradas, t.MinutosParado)));
    }

    [Fact]
    public async Task Disponibilidade_e_calculada_por_linha_sem_contar_escadas_rolantes()
    {
        var admin = await EntrarAsync(ApiFactory.Admin);
        var agora = DateTime.UtcNow;
        const int minutosDoPeriodo = 30 * 24 * 60;
        Api.NoBanco(db =>
        {
            db.Alertas.AddRange(
                Parada(Cenario.ElevadorDaPaulista, agora.AddDays(-5), minutos: minutosDoPeriodo / 10),
                Parada(Cenario.EscadaDaSe, agora.AddDays(-5), minutos: 9000));
            return db.SaveChanges();
        });

        var painel = await BuscarAsync<PainelResponse>(admin, "/api/painel");

        Assert.Equal(
            [new DisponibilidadeResponse(1, "Azul", 3, 100), new DisponibilidadeResponse(4, "Amarela", 1, 90)],
            painel.Disponibilidade);
    }

    private static Alerta Parada(int equipamentoId, DateTime inicio, int minutos) =>
        new() { EquipamentoId = equipamentoId, Motivo = "Parada de teste", AbertoEm = inicio, EncerradoEm = inicio.AddMinutes(minutos) };
}
