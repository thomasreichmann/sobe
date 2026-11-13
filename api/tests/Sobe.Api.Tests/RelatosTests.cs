using System.Net;
using System.Net.Http.Json;
using Sobe.Api.Dtos;
using Sobe.Api.Models;
using Sobe.Api.Tests.Infra;

namespace Sobe.Api.Tests;

public class RelatosTests : TesteDeApi
{
    private const string Descricao = "Elevador parado entre os andares.";

    private async Task<RelatoResponse> RelatarAsync(HttpClient cliente, string descricao = Descricao) =>
        await LerAsync<RelatoResponse>(
            await cliente.PostAsJsonAsync("/api/relatos", new NovoRelatoRequest(Cenario.ElevadorDaSe, descricao)));

    [Fact]
    public async Task Cidadao_registra_um_relato_que_nasce_aberto_e_ligado_ao_equipamento()
    {
        var ana = await EntrarAsync(ApiFactory.Ana);

        var resposta = await ana.PostAsJsonAsync("/api/relatos", new NovoRelatoRequest(Cenario.ElevadorDaSe, $"  {Descricao}  "));
        var relato = await LerAsync<RelatoResponse>(resposta);

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.EndsWith($"/api/relatos/{relato.Id}", resposta.Headers.Location!.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(Descricao, relato.Descricao);
        Assert.Equal(StatusRelato.Aberto, relato.Status);
        Assert.Equal("Ana", relato.Autor);
        Assert.True(relato.Meu);
        Assert.Equal("Sé", relato.Local.Estacao);
        Assert.Equal(TipoEquipamento.Elevador, relato.Local.Tipo);
    }

    [Theory]
    [InlineData("oi")]
    [InlineData("    ")]
    public async Task Relato_com_descricao_curta_demais_e_recusado(string descricao)
    {
        var ana = await EntrarAsync(ApiFactory.Ana);

        var resposta = await ana.PostAsJsonAsync("/api/relatos", new NovoRelatoRequest(Cenario.ElevadorDaSe, descricao));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal(0, Api.NoBanco(db => db.Relatos.Count()));
    }

    [Fact]
    public async Task Relato_com_mais_de_500_caracteres_e_recusado()
    {
        var ana = await EntrarAsync(ApiFactory.Ana);

        var noLimite = await ana.PostAsJsonAsync("/api/relatos", new NovoRelatoRequest(Cenario.ElevadorDaSe, new string('a', 500)));
        var acima = await ana.PostAsJsonAsync("/api/relatos", new NovoRelatoRequest(Cenario.ElevadorDaSe, new string('a', 501)));

        Assert.Equal(HttpStatusCode.Created, noLimite.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, acima.StatusCode);
    }

    [Fact]
    public async Task Relato_de_equipamento_que_nao_existe_responde_404()
    {
        var ana = await EntrarAsync(ApiFactory.Ana);

        var resposta = await ana.PostAsJsonAsync("/api/relatos", new NovoRelatoRequest(9999, Descricao));

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Cidadao_ve_so_os_proprios_relatos_e_o_administrador_ve_todos()
    {
        var ana = await EntrarAsync(ApiFactory.Ana);
        var bia = await EntrarAsync(ApiFactory.Bia);
        var admin = await EntrarAsync(ApiFactory.Admin);
        await RelatarAsync(ana, "Relato da Ana sobre o elevador.");
        await RelatarAsync(bia, "Relato da Bia sobre o elevador.");

        var daAna = await BuscarAsync<List<RelatoResponse>>(ana, "/api/relatos");
        var doAdmin = await BuscarAsync<List<RelatoResponse>>(admin, "/api/relatos");

        Assert.Equal(["Ana"], daAna.Select(r => r.Autor));
        Assert.Equal(["Ana", "Bia"], doAdmin.Select(r => r.Autor).Order());
        Assert.DoesNotContain(doAdmin, r => r.Meu);
    }

    [Fact]
    public async Task Relato_de_outra_pessoa_se_comporta_como_se_nao_existisse()
    {
        var ana = await EntrarAsync(ApiFactory.Ana);
        var bia = await EntrarAsync(ApiFactory.Bia);
        var daAna = await RelatarAsync(ana);

        var leitura = await bia.GetAsync($"/api/relatos/{daAna.Id}");
        var edicao = await bia.PutAsJsonAsync($"/api/relatos/{daAna.Id}", new EditarRelatoRequest("Tentativa de alterar o relato alheio."));
        var remocao = await bia.DeleteAsync($"/api/relatos/{daAna.Id}");

        Assert.Equal(HttpStatusCode.NotFound, leitura.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, edicao.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, remocao.StatusCode);
        Assert.Equal(Descricao, Api.NoBanco(db => db.Relatos.Single().Descricao));
    }

    [Fact]
    public async Task Autor_corrige_e_apaga_o_relato_enquanto_ele_esta_aberto()
    {
        var ana = await EntrarAsync(ApiFactory.Ana);
        var relato = await RelatarAsync(ana);

        var editado = await LerAsync<RelatoResponse>(
            await ana.PutAsJsonAsync($"/api/relatos/{relato.Id}", new EditarRelatoRequest("Elevador parado e com a porta aberta.")));
        var remocao = await ana.DeleteAsync($"/api/relatos/{relato.Id}");

        Assert.Equal("Elevador parado e com a porta aberta.", editado.Descricao);
        Assert.True(editado.AtualizadoEm >= relato.AtualizadoEm);
        Assert.Equal(HttpStatusCode.NoContent, remocao.StatusCode);
        Assert.Equal(0, Api.NoBanco(db => db.Relatos.Count()));
    }

    [Theory]
    [InlineData(StatusRelato.Confirmado)]
    [InlineData(StatusRelato.Resolvido)]
    [InlineData(StatusRelato.Descartado)]
    public async Task Depois_que_a_operadora_trata_o_relato_o_autor_nao_altera_mais(StatusRelato status)
    {
        var ana = await EntrarAsync(ApiFactory.Ana);
        var admin = await EntrarAsync(ApiFactory.Admin);
        var relato = await RelatarAsync(ana);

        var tratado = await LerAsync<RelatoResponse>(
            await admin.PutAsJsonAsync($"/api/relatos/{relato.Id}/status", new StatusRelatoRequest(status), Json));
        var edicao = await ana.PutAsJsonAsync($"/api/relatos/{relato.Id}", new EditarRelatoRequest("Quero mudar o que escrevi."));
        var remocao = await ana.DeleteAsync($"/api/relatos/{relato.Id}");

        Assert.Equal(status, tratado.Status);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, edicao.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, remocao.StatusCode);
        Assert.Equal(Descricao, Api.NoBanco(db => db.Relatos.Single().Descricao));
    }

    [Fact]
    public async Task Administrador_filtra_por_situacao_e_apaga_qualquer_relato()
    {
        var ana = await EntrarAsync(ApiFactory.Ana);
        var admin = await EntrarAsync(ApiFactory.Admin);
        var aberto = await RelatarAsync(ana, "Este relato continua aberto.");
        var resolvido = await RelatarAsync(ana, "Este relato vai ser resolvido.");
        await admin.PutAsJsonAsync($"/api/relatos/{resolvido.Id}/status", new StatusRelatoRequest(StatusRelato.Resolvido), Json);

        var abertos = await BuscarAsync<List<RelatoResponse>>(admin, "/api/relatos?status=ABERTO");
        var remocao = await admin.DeleteAsync($"/api/relatos/{resolvido.Id}");

        Assert.Equal([aberto.Id], abertos.Select(r => r.Id));
        Assert.Equal(HttpStatusCode.NoContent, remocao.StatusCode);
        Assert.Equal([aberto.Id], Api.NoBanco(db => db.Relatos.Select(r => r.Id).ToList()));
    }
}
