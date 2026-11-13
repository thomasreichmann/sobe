using Sobe.Api.Infra;
using Sobe.Api.Models;
using Sobe.Api.Security;

namespace Sobe.Api.Tests;

public class SenhaHasherTests
{
    [Fact]
    public void Mesma_senha_gera_hashes_diferentes_e_os_dois_conferem()
    {
        var primeiro = SenhaHasher.Gerar("Senha@123", iteracoes: 1000);
        var segundo = SenhaHasher.Gerar("Senha@123", iteracoes: 1000);

        Assert.NotEqual(primeiro, segundo);
        Assert.True(SenhaHasher.Conferir("Senha@123", primeiro));
        Assert.True(SenhaHasher.Conferir("Senha@123", segundo));
    }

    [Theory]
    [InlineData("senha@123")]
    [InlineData("Senha@1234")]
    [InlineData("")]
    public void Senha_diferente_nao_confere(string tentativa)
    {
        var hash = SenhaHasher.Gerar("Senha@123", iteracoes: 1000);

        Assert.False(SenhaHasher.Conferir(tentativa, hash));
    }

    [Theory]
    [InlineData("Senha@123")]
    [InlineData("MD5$1$c2FsdA==$aGFzaA==")]
    [InlineData("PBKDF2-SHA256$mil$c2FsdA==$aGFzaA==")]
    public void Valor_que_nao_e_um_hash_do_sistema_nunca_confere(string armazenado)
    {
        Assert.False(SenhaHasher.Conferir("Senha@123", armazenado));
    }

    [Theory]
    [InlineData("admin@sobe.app", "Admin@2026")]
    [InlineData("ana@sobe.app", "Cidadao@2026")]
    [InlineData("carlos@sobe.app", "Cidadao@2026")]
    public void Usuarios_da_carga_inicial_do_banco_entram_com_as_senhas_documentadas(string email, string senha)
    {
        var linha = File.ReadLines(CaminhoDoScript()).Single(l => l.StartsWith("INSERT INTO TB_USUARIO") && l.Contains($"'{email}'"));
        var hash = linha.Split('\'').Single(p => p.StartsWith("PBKDF2-SHA256$"));

        Assert.True(SenhaHasher.Conferir(senha, hash));
    }

    private static string CaminhoDoScript()
    {
        var pasta = new DirectoryInfo(AppContext.BaseDirectory);
        while (pasta is not null && !File.Exists(Path.Combine(pasta.FullName, "banco", "sobe.sql"))) pasta = pasta.Parent;
        return Path.Combine(pasta!.FullName, "banco", "sobe.sql");
    }
}

public class TextoTests
{
    [Theory]
    [InlineData("São Bento", "sao bento")]
    [InlineData("  PARAÍSO ", "paraiso")]
    [InlineData("Anhangabaú", "anhangabau")]
    [InlineData("Conceição", "conceicao")]
    public void Sem_acento_normaliza_para_comparar_nomes(string original, string esperado)
    {
        Assert.Equal(esperado, Texto.SemAcento(original));
    }
}

public class EstacaoTests
{
    private static Equipamento Equipamento(TipoEquipamento tipo, StatusEquipamento status) =>
        new() { Tipo = tipo, Localizacao = "Teste", Status = status };

    private static Estacao Estacao(params Equipamento[] equipamentos) =>
        new() { Nome = "Teste", Equipamentos = [.. equipamentos] };

    [Fact]
    public void Acessivel_quando_todos_os_elevadores_e_plataformas_operam()
    {
        var estacao = Estacao(
            Equipamento(TipoEquipamento.Elevador, StatusEquipamento.Operando),
            Equipamento(TipoEquipamento.Plataforma, StatusEquipamento.Operando),
            Equipamento(TipoEquipamento.EscadaRolante, StatusEquipamento.Parado));

        Assert.True(estacao.Acessivel);
        Assert.Equal(1, estacao.ForaDeServico);
    }

    [Theory]
    [InlineData(TipoEquipamento.Elevador, StatusEquipamento.Parado)]
    [InlineData(TipoEquipamento.Elevador, StatusEquipamento.Manutencao)]
    [InlineData(TipoEquipamento.Plataforma, StatusEquipamento.Parado)]
    public void Um_elevador_ou_plataforma_fora_de_servico_tira_a_acessibilidade(TipoEquipamento tipo, StatusEquipamento status)
    {
        var estacao = Estacao(Equipamento(TipoEquipamento.Elevador, StatusEquipamento.Operando), Equipamento(tipo, status));

        Assert.False(estacao.Acessivel);
    }

    [Fact]
    public void Estacao_so_com_escada_rolante_nao_e_acessivel()
    {
        var estacao = Estacao(Equipamento(TipoEquipamento.EscadaRolante, StatusEquipamento.Operando));

        Assert.False(estacao.Acessivel);
    }

    [Fact]
    public void Mudar_status_so_atualiza_a_data_quando_o_status_muda_de_fato()
    {
        var inicio = new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);
        var equipamento = Equipamento(TipoEquipamento.Elevador, StatusEquipamento.Operando);
        equipamento.StatusDesde = inicio;

        equipamento.MudarStatus(StatusEquipamento.Operando, inicio.AddHours(1));
        var semMudanca = equipamento.StatusDesde;
        equipamento.MudarStatus(StatusEquipamento.Parado, inicio.AddHours(2));

        Assert.Equal(inicio, semMudanca);
        Assert.Equal(inicio.AddHours(2), equipamento.StatusDesde);
    }
}
