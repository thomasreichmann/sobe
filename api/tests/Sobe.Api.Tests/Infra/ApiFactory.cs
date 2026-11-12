using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sobe.Api.Data;
using Sobe.Api.Models;
using Sobe.Api.Security;

namespace Sobe.Api.Tests.Infra;

public class ApiFactory : WebApplicationFactory<Program>
{
    public const string ChaveDosSensores = "chave-dos-sensores-de-teste";
    public const string Senha = "Senha@123";
    public const string Admin = "admin@teste.app";
    public const string Ana = "ana@teste.app";
    public const string Bia = "bia@teste.app";

    private readonly SqliteConnection conexao = new("DataSource=:memory:");

    public int LoginsPorMinuto { get; init; } = 1000;
    public Cenario Cenario { get; private set; } = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        conexao.Open();
        builder.UseEnvironment("Testing");
        builder.UseSetting("Jwt:Chave", "chave-de-teste-com-mais-de-32-bytes-0123456789");
        builder.UseSetting("Dispositivos:Chave", ChaveDosSensores);
        builder.UseSetting("Seguranca:LoginsPorMinuto", LoginsPorMinuto.ToString());
        builder.ConfigureTestServices(servicos =>
        {
            servicos.RemoveAll<DbContextOptions<SobeDbContext>>();
            servicos.AddDbContext<SobeDbContext>(opcoes => opcoes.UseSqlite(conexao));
        });
    }

    public void Semear()
    {
        using var escopo = Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<SobeDbContext>();
        db.Database.EnsureCreated();
        Cenario = Cenario.Criar(db);
    }

    public T NoBanco<T>(Func<SobeDbContext, T> consulta)
    {
        using var escopo = Services.CreateScope();
        return consulta(escopo.ServiceProvider.GetRequiredService<SobeDbContext>());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        conexao.Dispose();
    }
}

public record Cenario(
    int LinhaAzul, int LinhaAmarela, int Se, int Liberdade, int Paraiso, int Paulista,
    int ElevadorDaSe, int EscadaDaSe, int ElevadorDaLiberdade, int ElevadorDoParaiso, int ElevadorDaPaulista,
    int SensorDeMovimentoDaSe)
{
    public const string MovimentoDaSe = "SE-EL-MOV";
    public const string PortaDaSe = "SE-EL-PORTA";
    public const string MovimentoDaPaulista = "PAULISTA-EL-MOV";
    public const double LimiteDeMovimento = 15;
    public const double LimiteDePorta = 120;

    public static Cenario Criar(SobeDbContext db)
    {
        var agora = DateTime.UtcNow;
        var hash = SenhaHasher.Gerar(ApiFactory.Senha, iteracoes: 1000);
        db.Usuarios.AddRange(
            new Usuario { Nome = "Operadora", Email = ApiFactory.Admin, SenhaHash = hash, Perfil = Perfil.Admin, CriadoEm = agora },
            new Usuario { Nome = "Ana", Email = ApiFactory.Ana, SenhaHash = hash, CriadoEm = agora },
            new Usuario { Nome = "Bia", Email = ApiFactory.Bia, SenhaHash = hash, CriadoEm = agora });

        var azul = new Linha { Numero = 1, Nome = "Azul", Cor = "#0455A1", Operadora = "Metrô" };
        var amarela = new Linha { Numero = 4, Nome = "Amarela", Cor = "#FFD400", Operadora = "ViaQuatro" };

        var se = Estacao(azul, "Sé", 1);
        var liberdade = Estacao(azul, "Liberdade", 2);
        var paraiso = Estacao(azul, "Paraíso", 3);
        var paulista = Estacao(amarela, "Paulista", 1);

        var elevadorDaSe = Elevador(se, "SE-EL", agora);
        var escadaDaSe = new Equipamento
        {
            Estacao = se,
            Tipo = TipoEquipamento.EscadaRolante,
            Localizacao = "Do mezanino para a plataforma",
            StatusDesde = agora,
            Sensores = [Sensor(TipoSensor.Movimento, "SE-ESC-MOV")],
        };
        var elevadorDaLiberdade = Elevador(liberdade, "LIBERDADE-EL", agora);
        var elevadorDoParaiso = Elevador(paraiso, "PARAISO-EL", agora);
        var elevadorDaPaulista = Elevador(paulista, "PAULISTA-EL", agora);

        db.AddRange(elevadorDaSe, escadaDaSe, elevadorDaLiberdade, elevadorDoParaiso, elevadorDaPaulista);
        db.SaveChanges();

        return new Cenario(
            azul.Id, amarela.Id, se.Id, liberdade.Id, paraiso.Id, paulista.Id,
            elevadorDaSe.Id, escadaDaSe.Id, elevadorDaLiberdade.Id, elevadorDoParaiso.Id, elevadorDaPaulista.Id,
            elevadorDaSe.Sensores.Single(s => s.Tipo == TipoSensor.Movimento).Id);
    }

    private static Estacao Estacao(Linha linha, string nome, int ordem) => new() { Linha = linha, Nome = nome, Ordem = ordem };

    private static Equipamento Elevador(Estacao estacao, string prefixo, DateTime agora) => new()
    {
        Estacao = estacao,
        Tipo = TipoEquipamento.Elevador,
        Localizacao = "Entre a rua e a plataforma",
        StatusDesde = agora,
        Sensores = [Sensor(TipoSensor.Movimento, $"{prefixo}-MOV"), Sensor(TipoSensor.Porta, $"{prefixo}-PORTA")],
    };

    private static Sensor Sensor(TipoSensor tipo, string codigo)
    {
        var porta = tipo == TipoSensor.Porta;
        return new Sensor { Tipo = tipo, Codigo = codigo, Limite = porta ? LimiteDePorta : LimiteDeMovimento, Unidade = porta ? "s" : "min" };
    }
}
