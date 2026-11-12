using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Sobe.Api.Dtos;
using Sobe.Api.Tests.Infra;

namespace Sobe.Api.Tests;

public class ControleDeAcessoTests : TesteDeApi
{
    [Theory]
    [InlineData("GET", "/api/favoritos")]
    [InlineData("GET", "/api/alertas")]
    [InlineData("GET", "/api/relatos")]
    [InlineData("GET", "/api/auth/eu")]
    [InlineData("GET", "/api/painel")]
    [InlineData("GET", "/api/sensores")]
    [InlineData("DELETE", "/api/estacoes/1")]
    public async Task Sem_token_a_api_responde_401(string metodo, string caminho)
    {
        var resposta = await Anonimo().SendAsync(new HttpRequestMessage(new HttpMethod(metodo), caminho));

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Theory]
    [InlineData("POST", "/api/estacoes")]
    [InlineData("PUT", "/api/estacoes/1")]
    [InlineData("DELETE", "/api/estacoes/1")]
    [InlineData("POST", "/api/estacoes/1/equipamentos")]
    [InlineData("PUT", "/api/equipamentos/1")]
    [InlineData("DELETE", "/api/equipamentos/1")]
    [InlineData("PUT", "/api/sensores/1")]
    [InlineData("GET", "/api/sensores")]
    [InlineData("PUT", "/api/relatos/1/status")]
    [InlineData("GET", "/api/painel")]
    public async Task Cidadao_nao_entra_no_que_e_do_administrador(string metodo, string caminho)
    {
        var ana = await EntrarAsync(ApiFactory.Ana);
        var pedido = new HttpRequestMessage(new HttpMethod(metodo), caminho) { Content = JsonContent.Create(new { }) };

        var resposta = await ana.SendAsync(pedido);

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        Assert.Equal(5, Api.NoBanco(db => db.Equipamentos.Count()));
        Assert.Equal(4, Api.NoBanco(db => db.Estacoes.Count()));
    }

    [Fact]
    public async Task Token_com_o_perfil_adulterado_e_recusado()
    {
        var login = await LerAsync<LoginResponse>(
            await Anonimo().PostAsJsonAsync("/api/auth/login", new LoginRequest(ApiFactory.Ana, ApiFactory.Senha)));
        var partes = login.Token.Split('.');
        var corpo = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(partes[1].Replace('-', '+').Replace('_', '/').PadRight((partes[1].Length + 3) / 4 * 4, '=')));
        var corpoDeAdmin = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(corpo.Replace("CIDADAO", "ADMIN"))).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var cliente = Anonimo();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", $"{partes[0]}.{corpoDeAdmin}.{partes[2]}");

        var resposta = await cliente.GetAsync("/api/painel");

        Assert.Contains("CIDADAO", corpo);
        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Leitura_so_entra_com_a_chave_dos_sensores()
    {
        var leitura = new LeituraRequest(Cenario.MovimentoDaSe, 3);
        var admin = await EntrarAsync(ApiFactory.Admin);

        var semChave = await Anonimo().PostAsJsonAsync("/api/leituras", leitura);
        var chaveErrada = await Sensores("chave-que-nao-existe").PostAsJsonAsync("/api/leituras", leitura);
        var tokenDeAdmin = await admin.PostAsJsonAsync("/api/leituras", leitura);
        var chaveCerta = await Sensores().PostAsJsonAsync("/api/leituras", leitura);

        Assert.Equal(HttpStatusCode.Unauthorized, semChave.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, chaveErrada.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, tokenDeAdmin.StatusCode);
        Assert.Equal(HttpStatusCode.Created, chaveCerta.StatusCode);
        Assert.Equal(1, Api.NoBanco(db => db.Leituras.Count()));
    }

    [Fact]
    public async Task Chave_dos_sensores_nao_da_acesso_ao_resto_da_api()
    {
        var sensores = Sensores();

        var lista = await sensores.GetAsync("/api/sensores");
        var painel = await sensores.GetAsync("/api/painel");
        var relatos = await sensores.GetAsync("/api/relatos");

        Assert.Equal(HttpStatusCode.OK, lista.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, painel.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, relatos.StatusCode);
    }
}
