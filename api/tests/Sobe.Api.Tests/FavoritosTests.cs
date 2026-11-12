using System.Net;
using Sobe.Api.Dtos;
using Sobe.Api.Tests.Infra;

namespace Sobe.Api.Tests;

public class FavoritosTests : TesteDeApi
{
    [Fact]
    public async Task Favoritar_duas_vezes_guarda_uma_so_e_marca_a_estacao_na_lista()
    {
        var ana = await EntrarAsync(ApiFactory.Ana);

        var primeira = await ana.PutAsync($"/api/favoritos/{Cenario.Se}", null);
        var segunda = await ana.PutAsync($"/api/favoritos/{Cenario.Se}", null);

        Assert.Equal(HttpStatusCode.NoContent, primeira.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, segunda.StatusCode);
        Assert.Equal(["Sé"], (await BuscarAsync<List<EstacaoResumoResponse>>(ana, "/api/favoritos")).Select(e => e.Nome));
        var estacoes = await BuscarAsync<List<EstacaoResumoResponse>>(ana, "/api/estacoes");
        Assert.Equal([Cenario.Se], estacoes.Where(e => e.Favorita).Select(e => e.Id));
        Assert.True((await BuscarAsync<EstacaoDetalheResponse>(ana, $"/api/estacoes/{Cenario.Se}")).Favorita);
    }

    [Fact]
    public async Task Favoritos_de_um_usuario_nao_aparecem_para_outro()
    {
        var ana = await EntrarAsync(ApiFactory.Ana);
        var bia = await EntrarAsync(ApiFactory.Bia);

        await ana.PutAsync($"/api/favoritos/{Cenario.Se}", null);

        Assert.Empty(await BuscarAsync<List<EstacaoResumoResponse>>(bia, "/api/favoritos"));
        Assert.DoesNotContain(await BuscarAsync<List<EstacaoResumoResponse>>(bia, "/api/estacoes"), e => e.Favorita);
    }

    [Fact]
    public async Task Desfavoritar_tira_da_lista_e_repetir_nao_da_erro()
    {
        var ana = await EntrarAsync(ApiFactory.Ana);
        await ana.PutAsync($"/api/favoritos/{Cenario.Se}", null);
        await ana.PutAsync($"/api/favoritos/{Cenario.Paulista}", null);

        var primeira = await ana.DeleteAsync($"/api/favoritos/{Cenario.Se}");
        var segunda = await ana.DeleteAsync($"/api/favoritos/{Cenario.Se}");

        Assert.Equal(HttpStatusCode.NoContent, primeira.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, segunda.StatusCode);
        Assert.Equal(["Paulista"], (await BuscarAsync<List<EstacaoResumoResponse>>(ana, "/api/favoritos")).Select(e => e.Nome));
    }

    [Fact]
    public async Task Favoritar_estacao_que_nao_existe_responde_404()
    {
        var ana = await EntrarAsync(ApiFactory.Ana);

        var resposta = await ana.PutAsync("/api/favoritos/9999", null);

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }
}
