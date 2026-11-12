using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Sobe.Api.Dtos;
using Sobe.Api.Security;

namespace Sobe.Api.Tests.Infra;

public abstract class TesteDeApi : IDisposable
{
    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper) },
    };

    protected readonly ApiFactory Api;

    protected TesteDeApi(ApiFactory? api = null)
    {
        Api = api ?? new ApiFactory();
        Api.Semear();
    }

    protected Cenario Cenario => Api.Cenario;

    public void Dispose()
    {
        Api.Dispose();
        GC.SuppressFinalize(this);
    }

    protected HttpClient Anonimo() => Api.CreateClient();

    protected async Task<HttpClient> EntrarAsync(string email)
    {
        var cliente = Api.CreateClient();
        var resposta = await cliente.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, ApiFactory.Senha));
        resposta.EnsureSuccessStatusCode();
        var login = await LerAsync<LoginResponse>(resposta);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
        return cliente;
    }

    protected HttpClient Sensores(string chave = ApiFactory.ChaveDosSensores)
    {
        var cliente = Api.CreateClient();
        cliente.DefaultRequestHeaders.Add(DispositivoAuthHandler.Cabecalho, chave);
        return cliente;
    }

    protected async Task<LeituraResponse> LeituraAsync(string sensor, double valor)
    {
        var resposta = await Sensores().PostAsJsonAsync("/api/leituras", new LeituraRequest(sensor, valor));
        resposta.EnsureSuccessStatusCode();
        return await LerAsync<LeituraResponse>(resposta);
    }

    protected static async Task<T> LerAsync<T>(HttpResponseMessage resposta) =>
        (await resposta.Content.ReadFromJsonAsync<T>(Json))!;

    protected static async Task<T> BuscarAsync<T>(HttpClient cliente, string caminho) =>
        (await cliente.GetFromJsonAsync<T>(caminho, Json))!;
}
