using System.Net;
using System.Net.Http.Json;
using Sobe.Api.Dtos;
using Sobe.Api.Models;
using Sobe.Api.Tests.Infra;

namespace Sobe.Api.Tests;

public class EstacoesTests : TesteDeApi
{
    [Fact]
    public async Task Lista_vem_ordenada_por_linha_e_posicao_e_sem_exigir_login()
    {
        var estacoes = await BuscarAsync<List<EstacaoResumoResponse>>(Anonimo(), "/api/estacoes");

        Assert.Equal(["Sé", "Liberdade", "Paraíso", "Paulista"], estacoes.Select(e => e.Nome));
        var se = estacoes[0];
        Assert.Equal(2, se.Equipamentos);
        Assert.Equal(0, se.ForaDeServico);
        Assert.True(se.Acessivel);
        Assert.False(se.Favorita);
    }

    [Theory]
    [InlineData("se", "Sé")]
    [InlineData("PARAISO", "Paraíso")]
    [InlineData(" paraí ", "Paraíso")]
    public async Task Busca_ignora_acentos_e_maiusculas(string busca, string esperada)
    {
        var estacoes = await BuscarAsync<List<EstacaoResumoResponse>>(Anonimo(), $"/api/estacoes?busca={Uri.EscapeDataString(busca)}");

        Assert.Equal([esperada], estacoes.Select(e => e.Nome));
    }

    [Fact]
    public async Task Filtro_por_linha_traz_so_as_estacoes_dela()
    {
        var estacoes = await BuscarAsync<List<EstacaoResumoResponse>>(Anonimo(), $"/api/estacoes?linhaId={Cenario.LinhaAmarela}");

        Assert.Equal(["Paulista"], estacoes.Select(e => e.Nome));
    }

    [Fact]
    public async Task Estacao_que_nao_existe_responde_404()
    {
        var resposta = await Anonimo().GetAsync("/api/estacoes/9999");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Administrador_cadastra_altera_e_remove_uma_estacao()
    {
        var admin = await EntrarAsync(ApiFactory.Admin);

        var criacao = await admin.PostAsJsonAsync("/api/estacoes", new EstacaoRequest(Cenario.LinhaAmarela, "  Pinheiros ", 2));
        var criada = await LerAsync<EstacaoDetalheResponse>(criacao);
        var alterada = await LerAsync<EstacaoDetalheResponse>(
            await admin.PutAsJsonAsync($"/api/estacoes/{criada.Id}", new EstacaoRequest(Cenario.LinhaAmarela, "Pinheiros", 5)));
        var remocao = await admin.DeleteAsync($"/api/estacoes/{criada.Id}");
        var depois = await admin.GetAsync($"/api/estacoes/{criada.Id}");

        Assert.Equal(HttpStatusCode.Created, criacao.StatusCode);
        Assert.EndsWith($"/api/estacoes/{criada.Id}", criacao.Headers.Location!.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Pinheiros", criada.Nome);
        Assert.Equal(5, alterada.Ordem);
        Assert.Equal(HttpStatusCode.NoContent, remocao.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, depois.StatusCode);
    }

    [Theory]
    [InlineData("Sé", 9)]
    [InlineData("Outra", 1)]
    public async Task Linha_nao_aceita_nome_nem_posicao_repetidos(string nome, int ordem)
    {
        var admin = await EntrarAsync(ApiFactory.Admin);

        var resposta = await admin.PostAsJsonAsync("/api/estacoes", new EstacaoRequest(Cenario.LinhaAzul, nome, ordem));

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Equal(4, Api.NoBanco(db => db.Estacoes.Count()));
    }

    [Fact]
    public async Task Mesmo_nome_pode_existir_em_linhas_diferentes()
    {
        var admin = await EntrarAsync(ApiFactory.Admin);

        var resposta = await admin.PostAsJsonAsync("/api/estacoes", new EstacaoRequest(Cenario.LinhaAmarela, "Sé", 2));

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
    }

    [Fact]
    public async Task Alterar_uma_estacao_mantendo_nome_e_posicao_nao_conflita_com_ela_mesma()
    {
        var admin = await EntrarAsync(ApiFactory.Admin);

        var resposta = await admin.PutAsJsonAsync($"/api/estacoes/{Cenario.Se}", new EstacaoRequest(Cenario.LinhaAzul, "Sé", 1));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Theory]
    [InlineData(TipoEquipamento.Elevador, new[] { TipoSensor.Movimento, TipoSensor.Porta })]
    [InlineData(TipoEquipamento.Plataforma, new[] { TipoSensor.Movimento, TipoSensor.Porta })]
    [InlineData(TipoEquipamento.EscadaRolante, new[] { TipoSensor.Movimento })]
    public async Task Equipamento_novo_ja_nasce_com_os_sensores_do_tipo_dele(TipoEquipamento tipo, TipoSensor[] esperados)
    {
        var admin = await EntrarAsync(ApiFactory.Admin);

        var resposta = await admin.PostAsJsonAsync(
            $"/api/estacoes/{Cenario.Paulista}/equipamentos", new NovoEquipamentoRequest(tipo, "Acesso da Consolação"), Json);
        var criado = await LerAsync<EquipamentoResponse>(resposta);

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.Equal(StatusEquipamento.Operando, criado.Status);
        var sensores = Api.NoBanco(db => db.Sensores.Where(s => s.EquipamentoId == criado.Id).OrderBy(s => s.Tipo).ToList());
        Assert.Equal(esperados, sensores.Select(s => s.Tipo).Order());
        Assert.All(sensores, s => Assert.StartsWith($"EQ{criado.Id:000}-", s.Codigo));
    }

    [Fact]
    public async Task Remover_equipamento_leva_junto_sensores_e_alertas()
    {
        var admin = await EntrarAsync(ApiFactory.Admin);
        await LeituraAsync(Cenario.MovimentoDaSe, 40);

        var resposta = await admin.DeleteAsync($"/api/equipamentos/{Cenario.ElevadorDaSe}");

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        Assert.Equal(0, Api.NoBanco(db => db.Sensores.Count(s => s.EquipamentoId == Cenario.ElevadorDaSe)));
        Assert.Equal(0, Api.NoBanco(db => db.Alertas.Count()));
        Assert.Equal(0, Api.NoBanco(db => db.Leituras.Count()));
    }
}
