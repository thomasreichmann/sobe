using Sobe.Api.Models;

namespace Sobe.Api.Dtos;

public record UsuarioResponse(int Id, string Nome, string Email, Perfil Perfil);

public record LoginResponse(string Token, DateTime ExpiraEm, UsuarioResponse Usuario);

public record LinhaResponse(int Id, int Numero, string Nome, string Cor, string Operadora);

public record EstacaoResumoResponse(
    int Id, string Nome, int Ordem, LinhaResponse Linha, int Equipamentos, int ForaDeServico, bool Acessivel, bool Favorita);

public record AlertaDoEquipamento(int Id, string Motivo, DateTime AbertoEm);

public record EquipamentoResponse(
    int Id, int EstacaoId, TipoEquipamento Tipo, string Localizacao, StatusEquipamento Status, DateTime StatusDesde,
    AlertaDoEquipamento? Alerta);

public record AlternativaResponse(int Id, string Nome, int EstacoesDeDistancia);

public record EstacaoDetalheResponse(
    int Id, string Nome, int Ordem, LinhaResponse Linha, bool Acessivel, bool Favorita,
    IReadOnlyList<EquipamentoResponse> Equipamentos, AlternativaResponse? Alternativa);

public record LocalResponse(
    int EquipamentoId, TipoEquipamento Tipo, string Localizacao, int EstacaoId, string Estacao, int LinhaNumero,
    string LinhaNome, string LinhaCor);

public record AlertaResponse(int Id, string Motivo, DateTime AbertoEm, DateTime? EncerradoEm, LocalResponse Local);

public record RelatoResponse(
    int Id, string Descricao, StatusRelato Status, DateTime CriadoEm, DateTime AtualizadoEm, string Autor, bool Meu,
    LocalResponse Local);

public record SensorResponse(
    int Id, string Codigo, TipoSensor Tipo, double Limite, string Unidade, bool Ativo, LocalResponse Local);

public record LeituraResponse(
    long Id, string Sensor, double Valor, double Limite, bool AcimaDoLimite, DateTime RegistradaEm,
    StatusEquipamento StatusDoEquipamento, int? AlertaAbertoId);

public record TempoParadoResponse(LocalResponse Local, int Paradas, int MinutosParado);

public record DisponibilidadeResponse(int LinhaNumero, string LinhaNome, int Equipamentos, double Percentual);

public record PainelResponse(
    int Equipamentos, int Operando, int Parados, int EmManutencao, int AlertasAbertos, int RelatosAbertos,
    IReadOnlyList<TempoParadoResponse> MaisParados, IReadOnlyList<DisponibilidadeResponse> Disponibilidade);
