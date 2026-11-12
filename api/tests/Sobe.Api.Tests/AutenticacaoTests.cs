using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Sobe.Api.Dtos;
using Sobe.Api.Models;
using Sobe.Api.Tests.Infra;

namespace Sobe.Api.Tests;

public class AutenticacaoTests : TesteDeApi
{
    [Fact]
    public async Task Cadastro_cria_um_cidadao_e_o_token_devolvido_ja_funciona()
    {
        var cliente = Anonimo();

        var resposta = await cliente.PostAsJsonAsync("/api/auth/registrar", new RegistrarRequest("Carla Dias", "Carla@Teste.app", "Carla@2026"));

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var login = await LerAsync<LoginResponse>(resposta);
        Assert.Equal("carla@teste.app", login.Usuario.Email);
        Assert.Equal(Perfil.Cidadao, login.Usuario.Perfil);

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
        var eu = await BuscarAsync<UsuarioResponse>(cliente, "/api/auth/eu");
        Assert.Equal("Carla Dias", eu.Nome);
    }

    [Fact]
    public async Task Cadastro_recusa_email_que_ja_tem_conta()
    {
        var resposta = await Anonimo().PostAsJsonAsync("/api/auth/registrar", new RegistrarRequest("Outra Ana", ApiFactory.Ana.ToUpperInvariant(), "Outra@2026"));

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Theory]
    [InlineData("Carla", "carla@teste.app", "curta1")]
    [InlineData("Carla", "carla@teste.app", "somenteletras")]
    [InlineData("Carla", "carla@teste.app", "1234567890")]
    [InlineData("Carla", "sem-arroba", "Carla@2026")]
    [InlineData("C", "carla@teste.app", "Carla@2026")]
    [InlineData("   ", "carla@teste.app", "Carla@2026")]
    public async Task Cadastro_recusa_dados_invalidos(string nome, string email, string senha)
    {
        var resposta = await Anonimo().PostAsJsonAsync("/api/auth/registrar", new RegistrarRequest(nome, email, senha));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal(0, Api.NoBanco(db => db.Usuarios.Count(u => u.Email == "carla@teste.app")));
    }

    [Fact]
    public async Task Senha_fica_no_banco_como_hash_com_salt()
    {
        await Anonimo().PostAsJsonAsync("/api/auth/registrar", new RegistrarRequest("Carla", "carla@teste.app", "Carla@2026"));
        await Anonimo().PostAsJsonAsync("/api/auth/registrar", new RegistrarRequest("Dora", "dora@teste.app", "Carla@2026"));

        var hashes = Api.NoBanco(db => db.Usuarios.Where(u => u.Nome == "Carla" || u.Nome == "Dora").Select(u => u.SenhaHash).ToList());

        Assert.All(hashes, h => Assert.StartsWith("PBKDF2-SHA256$600000$", h));
        Assert.All(hashes, h => Assert.DoesNotContain("Carla@2026", h));
        Assert.NotEqual(hashes[0], hashes[1]);
    }

    [Fact]
    public async Task Login_devolve_token_e_perfil()
    {
        var resposta = await Anonimo().PostAsJsonAsync("/api/auth/login", new LoginRequest(ApiFactory.Admin, ApiFactory.Senha));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var login = await LerAsync<LoginResponse>(resposta);
        Assert.Equal(Perfil.Admin, login.Usuario.Perfil);
        Assert.True(login.ExpiraEm > DateTime.UtcNow);
    }

    [Theory]
    [InlineData(ApiFactory.Ana, "Senha@errada1")]
    [InlineData("ninguem@teste.app", ApiFactory.Senha)]
    public async Task Login_recusa_credenciais_erradas_sem_dizer_qual_campo_errou(string email, string senha)
    {
        var resposta = await Anonimo().PostAsJsonAsync("/api/auth/login", new LoginRequest(email, senha));

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        Assert.Contains("E-mail ou senha incorretos.", await resposta.Content.ReadAsStringAsync());
    }
}

public class LimiteDeLoginTests() : TesteDeApi(new ApiFactory { LoginsPorMinuto = 3 })
{
    [Fact]
    public async Task Login_bloqueia_depois_de_tentativas_demais_no_mesmo_minuto()
    {
        var cliente = Anonimo();
        var tentativa = new LoginRequest(ApiFactory.Ana, "Senha@errada1");

        for (var i = 0; i < 3; i++)
        {
            var recusada = await cliente.PostAsJsonAsync("/api/auth/login", tentativa);
            Assert.Equal(HttpStatusCode.Unauthorized, recusada.StatusCode);
        }
        var bloqueada = await cliente.PostAsJsonAsync("/api/auth/login", new LoginRequest(ApiFactory.Ana, ApiFactory.Senha));

        Assert.Equal(HttpStatusCode.TooManyRequests, bloqueada.StatusCode);
    }
}
