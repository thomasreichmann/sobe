using System.Net;
using System.Net.Http.Json;
using Sobe.Api.Dtos;
using Sobe.Api.Models;
using Sobe.Api.Tests.Infra;

namespace Sobe.Api.Tests;

public class SensoresTests : TesteDeApi
{
    [Fact]
    public async Task Leitura_dentro_do_limite_e_guardada_e_nao_muda_nada()
    {
        var leitura = await LeituraAsync(Cenario.MovimentoDaSe, 4);

        Assert.False(leitura.AcimaDoLimite);
        Assert.Equal(StatusEquipamento.Operando, leitura.StatusDoEquipamento);
        Assert.Null(leitura.AlertaAbertoId);
        Assert.Equal(4, Api.NoBanco(db => db.Leituras.Single().Valor));
        Assert.Equal(0, Api.NoBanco(db => db.Alertas.Count()));
    }

    [Theory]
    [InlineData(Cenario.MovimentoDaSe, Cenario.LimiteDeMovimento, false)]
    [InlineData(Cenario.MovimentoDaSe, Cenario.LimiteDeMovimento + 0.01, true)]
    [InlineData(Cenario.PortaDaSe, Cenario.LimiteDePorta, false)]
    [InlineData(Cenario.PortaDaSe, Cenario.LimiteDePorta + 1, true)]
    public async Task Alerta_so_abre_quando_a_leitura_passa_do_limite(string sensor, double valor, bool deveAbrir)
    {
        var leitura = await LeituraAsync(sensor, valor);

        Assert.Equal(deveAbrir, leitura.AcimaDoLimite);
        Assert.Equal(deveAbrir, leitura.AlertaAbertoId is not null);
        Assert.Equal(deveAbrir ? StatusEquipamento.Parado : StatusEquipamento.Operando, leitura.StatusDoEquipamento);
    }

    [Fact]
    public async Task Parada_aparece_para_o_cidadao_com_o_motivo_e_a_estacao_alternativa()
    {
        var ana = await EntrarAsync(ApiFactory.Ana);

        await LeituraAsync(Cenario.MovimentoDaSe, 22);

        var alerta = Assert.Single(await BuscarAsync<List<AlertaResponse>>(ana, "/api/alertas"));
        Assert.Equal("Sem movimento há 22 min (limite de 15 min)", alerta.Motivo);
        Assert.Equal("Sé", alerta.Local.Estacao);
        Assert.Equal(Cenario.ElevadorDaSe, alerta.Local.EquipamentoId);
        Assert.Null(alerta.EncerradoEm);

        var se = await BuscarAsync<EstacaoDetalheResponse>(ana, $"/api/estacoes/{Cenario.Se}");
        Assert.False(se.Acessivel);
        Assert.Equal(alerta.Id, se.Equipamentos.Single(q => q.Id == Cenario.ElevadorDaSe).Alerta!.Id);
        Assert.Equal(new AlternativaResponse(Cenario.Liberdade, "Liberdade", 1), se.Alternativa);
    }

    [Fact]
    public async Task Porta_travada_descreve_o_motivo_em_segundos()
    {
        await LeituraAsync(Cenario.PortaDaSe, 300);

        Assert.Equal("Porta aberta há 300 s (limite de 120 s)", Api.NoBanco(db => db.Alertas.Single().Motivo));
    }

    [Fact]
    public async Task Alternativa_pula_estacoes_que_tambem_estao_sem_elevador()
    {
        await LeituraAsync(Cenario.MovimentoDaSe, 30);
        await LeituraAsync("LIBERDADE-EL-MOV", 30);

        var se = await BuscarAsync<EstacaoDetalheResponse>(Anonimo(), $"/api/estacoes/{Cenario.Se}");

        Assert.Equal(new AlternativaResponse(Cenario.Paraiso, "Paraíso", 2), se.Alternativa);
    }

    [Fact]
    public async Task Estacao_acessivel_nao_recebe_alternativa()
    {
        var se = await BuscarAsync<EstacaoDetalheResponse>(Anonimo(), $"/api/estacoes/{Cenario.Se}");

        Assert.True(se.Acessivel);
        Assert.Null(se.Alternativa);
    }

    [Fact]
    public async Task Escada_rolante_parada_gera_alerta_mas_nao_tira_a_acessibilidade()
    {
        await LeituraAsync("SE-ESC-MOV", 60);

        var se = await BuscarAsync<EstacaoDetalheResponse>(Anonimo(), $"/api/estacoes/{Cenario.Se}");
        var resumo = (await BuscarAsync<List<EstacaoResumoResponse>>(Anonimo(), "/api/estacoes")).Single(e => e.Id == Cenario.Se);

        Assert.Equal(StatusEquipamento.Parado, se.Equipamentos.Single(q => q.Id == Cenario.EscadaDaSe).Status);
        Assert.True(se.Acessivel);
        Assert.Equal(1, resumo.ForaDeServico);
    }

    [Fact]
    public async Task Leituras_altas_seguidas_mantem_um_unico_alerta()
    {
        var primeira = await LeituraAsync(Cenario.MovimentoDaSe, 20);
        var segunda = await LeituraAsync(Cenario.MovimentoDaSe, 25);
        var outroSensor = await LeituraAsync(Cenario.PortaDaSe, 500);

        Assert.Equal(primeira.AlertaAbertoId, segunda.AlertaAbertoId);
        Assert.Equal(primeira.AlertaAbertoId, outroSensor.AlertaAbertoId);
        Assert.Equal(1, Api.NoBanco(db => db.Alertas.Count()));
    }

    [Fact]
    public async Task Equipamento_so_volta_quando_todos_os_sensores_estao_normais()
    {
        await LeituraAsync(Cenario.MovimentoDaSe, 20);
        await LeituraAsync(Cenario.PortaDaSe, 300);

        var movimentoNormal = await LeituraAsync(Cenario.MovimentoDaSe, 2);
        var portaNormal = await LeituraAsync(Cenario.PortaDaSe, 10);

        Assert.Equal(StatusEquipamento.Parado, movimentoNormal.StatusDoEquipamento);
        Assert.NotNull(movimentoNormal.AlertaAbertoId);
        Assert.Equal(StatusEquipamento.Operando, portaNormal.StatusDoEquipamento);
        Assert.Null(portaNormal.AlertaAbertoId);
        var alerta = Api.NoBanco(db => db.Alertas.Single());
        Assert.NotNull(alerta.EncerradoEm);
        Assert.True(alerta.EncerradoEm >= alerta.AbertoEm);
    }

    [Fact]
    public async Task Nova_parada_depois_da_normalizacao_abre_outro_alerta()
    {
        var primeira = await LeituraAsync(Cenario.MovimentoDaSe, 20);
        await LeituraAsync(Cenario.MovimentoDaSe, 1);
        var segunda = await LeituraAsync(Cenario.MovimentoDaSe, 20);

        Assert.NotEqual(primeira.AlertaAbertoId, segunda.AlertaAbertoId);
        Assert.Equal(1, Api.NoBanco(db => db.Alertas.Count(a => a.EncerradoEm == null)));
        Assert.Equal(2, Api.NoBanco(db => db.Alertas.Count()));
    }

    [Fact]
    public async Task Em_manutencao_as_leituras_sao_guardadas_mas_nao_geram_alerta()
    {
        var admin = await EntrarAsync(ApiFactory.Admin);
        await admin.PutAsJsonAsync($"/api/equipamentos/{Cenario.ElevadorDaSe}", new EquipamentoRequest("Entre a rua e a plataforma", StatusEquipamento.Manutencao), Json);

        var leitura = await LeituraAsync(Cenario.MovimentoDaSe, 90);

        Assert.True(leitura.AcimaDoLimite);
        Assert.Equal(StatusEquipamento.Manutencao, leitura.StatusDoEquipamento);
        Assert.Equal(0, Api.NoBanco(db => db.Alertas.Count()));
        Assert.Equal(1, Api.NoBanco(db => db.Leituras.Count()));
    }

    [Fact]
    public async Task Parada_registrada_pela_operadora_nao_e_desfeita_por_leitura_normal()
    {
        var admin = await EntrarAsync(ApiFactory.Admin);
        var parado = await LerAsync<EquipamentoResponse>(await admin.PutAsJsonAsync(
            $"/api/equipamentos/{Cenario.ElevadorDaSe}", new EquipamentoRequest("Entre a rua e a plataforma", StatusEquipamento.Parado), Json));

        var leitura = await LeituraAsync(Cenario.MovimentoDaSe, 1);

        Assert.Equal("Parada registrada pela operadora", parado.Alerta!.Motivo);
        Assert.Equal(StatusEquipamento.Parado, leitura.StatusDoEquipamento);
        Assert.Equal(parado.Alerta.Id, leitura.AlertaAbertoId);
    }

    [Fact]
    public async Task Operadora_liberar_o_equipamento_encerra_o_alerta()
    {
        var admin = await EntrarAsync(ApiFactory.Admin);
        await LeituraAsync(Cenario.MovimentoDaSe, 20);

        var liberado = await LerAsync<EquipamentoResponse>(await admin.PutAsJsonAsync(
            $"/api/equipamentos/{Cenario.ElevadorDaSe}", new EquipamentoRequest("Entre a rua e a plataforma", StatusEquipamento.Operando), Json));

        Assert.Equal(StatusEquipamento.Operando, liberado.Status);
        Assert.Null(liberado.Alerta);
        Assert.Equal(0, Api.NoBanco(db => db.Alertas.Count(a => a.EncerradoEm == null)));
    }

    [Fact]
    public async Task Limite_ajustado_pelo_administrador_passa_a_valer_na_leitura_seguinte()
    {
        var admin = await EntrarAsync(ApiFactory.Admin);
        var antes = await LeituraAsync(Cenario.MovimentoDaSe, 10);

        var ajuste = await admin.PutAsJsonAsync($"/api/sensores/{Cenario.SensorDeMovimentoDaSe}", new SensorRequest(5, true));
        var depois = await LeituraAsync(Cenario.MovimentoDaSe, 10);

        Assert.Equal(HttpStatusCode.OK, ajuste.StatusCode);
        Assert.False(antes.AcimaDoLimite);
        Assert.True(depois.AcimaDoLimite);
        Assert.Equal("Sem movimento há 10 min (limite de 5 min)", Api.NoBanco(db => db.Alertas.Single().Motivo));
    }

    [Fact]
    public async Task Sensor_desativado_tem_a_leitura_recusada()
    {
        var admin = await EntrarAsync(ApiFactory.Admin);
        await admin.PutAsJsonAsync($"/api/sensores/{Cenario.SensorDeMovimentoDaSe}", new SensorRequest(15, false));

        var resposta = await Sensores().PostAsJsonAsync("/api/leituras", new LeituraRequest(Cenario.MovimentoDaSe, 50));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
        Assert.Equal(0, Api.NoBanco(db => db.Leituras.Count()));
    }

    [Fact]
    public async Task Sensor_desativado_nao_segura_o_equipamento_parado()
    {
        var admin = await EntrarAsync(ApiFactory.Admin);
        await LeituraAsync(Cenario.MovimentoDaSe, 20);
        await LeituraAsync(Cenario.PortaDaSe, 300);
        await admin.PutAsJsonAsync($"/api/sensores/{Cenario.SensorDeMovimentoDaSe}", new SensorRequest(15, false));

        var portaNormal = await LeituraAsync(Cenario.PortaDaSe, 10);

        Assert.Equal(StatusEquipamento.Operando, portaNormal.StatusDoEquipamento);
    }

    [Theory]
    [InlineData("NAO-EXISTE", 5, HttpStatusCode.NotFound)]
    [InlineData(Cenario.MovimentoDaSe, -1, HttpStatusCode.BadRequest)]
    [InlineData(Cenario.MovimentoDaSe, 100001, HttpStatusCode.BadRequest)]
    [InlineData("", 5, HttpStatusCode.BadRequest)]
    public async Task Leitura_invalida_e_recusada_e_nao_e_guardada(string sensor, double valor, HttpStatusCode esperado)
    {
        var resposta = await Sensores().PostAsJsonAsync("/api/leituras", new LeituraRequest(sensor, valor));

        Assert.Equal(esperado, resposta.StatusCode);
        Assert.Equal(0, Api.NoBanco(db => db.Leituras.Count()));
    }

    [Fact]
    public async Task Codigo_do_sensor_aceita_minusculas_e_espacos()
    {
        var leitura = await LeituraAsync($"  {Cenario.MovimentoDaSe.ToLowerInvariant()} ", 3);

        Assert.Equal(Cenario.MovimentoDaSe, leitura.Sensor);
    }

    [Fact]
    public async Task Alertas_das_favoritas_trazem_so_as_estacoes_que_o_usuario_acompanha()
    {
        var ana = await EntrarAsync(ApiFactory.Ana);
        await ana.PutAsync($"/api/favoritos/{Cenario.Paulista}", null);
        await LeituraAsync(Cenario.MovimentoDaSe, 20);
        await LeituraAsync(Cenario.MovimentoDaPaulista, 20);

        var todos = await BuscarAsync<List<AlertaResponse>>(ana, "/api/alertas");
        var dasFavoritas = await BuscarAsync<List<AlertaResponse>>(ana, "/api/alertas?somenteFavoritas=true");

        Assert.Equal(2, todos.Count);
        Assert.Equal(["Paulista"], dasFavoritas.Select(a => a.Local.Estacao));
    }

    [Fact]
    public async Task Alertas_encerrados_so_aparecem_quando_pedidos()
    {
        var ana = await EntrarAsync(ApiFactory.Ana);
        await LeituraAsync(Cenario.MovimentoDaSe, 20);
        await LeituraAsync(Cenario.MovimentoDaSe, 1);
        await LeituraAsync(Cenario.MovimentoDaPaulista, 20);

        var abertos = await BuscarAsync<List<AlertaResponse>>(ana, "/api/alertas");
        var historico = await BuscarAsync<List<AlertaResponse>>(ana, "/api/alertas?somenteAbertos=false");

        Assert.Equal(["Paulista"], abertos.Select(a => a.Local.Estacao));
        Assert.Equal(["Paulista", "Sé"], historico.Select(a => a.Local.Estacao));
    }

    [Fact]
    public async Task Lista_de_sensores_traz_codigo_limite_e_onde_cada_um_esta()
    {
        var sensores = await BuscarAsync<List<SensorResponse>>(Sensores(), "/api/sensores");

        Assert.Equal(9, sensores.Count);
        var porta = sensores.Single(s => s.Codigo == Cenario.PortaDaSe);
        Assert.Equal(TipoSensor.Porta, porta.Tipo);
        Assert.Equal(Cenario.LimiteDePorta, porta.Limite);
        Assert.Equal("s", porta.Unidade);
        Assert.Equal("Sé", porta.Local.Estacao);
        Assert.Equal(1, porta.Local.LinhaNumero);
    }
}
