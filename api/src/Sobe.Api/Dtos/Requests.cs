using System.ComponentModel.DataAnnotations;
using Sobe.Api.Models;

namespace Sobe.Api.Dtos;

public record RegistrarRequest(
    [Required(ErrorMessage = "Informe o nome.")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "O nome precisa ter de 2 a 120 caracteres.")]
    string Nome,
    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    [StringLength(160, ErrorMessage = "O e-mail pode ter até 160 caracteres.")]
    string Email,
    [Required(ErrorMessage = "Informe a senha.")]
    [StringLength(72, MinimumLength = 8, ErrorMessage = "A senha precisa ter de 8 a 72 caracteres.")]
    [RegularExpression(@"^(?=.*\p{L})(?=.*\d).+$", ErrorMessage = "A senha precisa ter pelo menos uma letra e um número.")]
    string Senha);

public record LoginRequest(
    [Required(ErrorMessage = "Informe o e-mail.")]
    [StringLength(160, ErrorMessage = "O e-mail pode ter até 160 caracteres.")]
    string Email,
    [Required(ErrorMessage = "Informe a senha.")]
    [StringLength(72, ErrorMessage = "A senha pode ter até 72 caracteres.")]
    string Senha);

public record EstacaoRequest(
    [Range(1, int.MaxValue, ErrorMessage = "Informe a linha.")]
    int LinhaId,
    [Required(ErrorMessage = "Informe o nome da estação.")]
    [StringLength(80, MinimumLength = 2, ErrorMessage = "O nome precisa ter de 2 a 80 caracteres.")]
    string Nome,
    [Range(1, 999, ErrorMessage = "A ordem na linha vai de 1 a 999.")]
    int Ordem);

public record NovoEquipamentoRequest(
    [Required(ErrorMessage = "Informe o tipo do equipamento.")]
    TipoEquipamento? Tipo,
    [Required(ErrorMessage = "Informe onde o equipamento fica.")]
    [StringLength(120, MinimumLength = 3, ErrorMessage = "A localização precisa ter de 3 a 120 caracteres.")]
    string Localizacao);

public record EquipamentoRequest(
    [Required(ErrorMessage = "Informe onde o equipamento fica.")]
    [StringLength(120, MinimumLength = 3, ErrorMessage = "A localização precisa ter de 3 a 120 caracteres.")]
    string Localizacao,
    [Required(ErrorMessage = "Informe a situação do equipamento.")]
    StatusEquipamento? Status);

public record SensorRequest(
    [Range(0.01, 100000, ErrorMessage = "O limite precisa ser maior que zero.")]
    double Limite,
    bool Ativo);

public record LeituraRequest(
    [Required(ErrorMessage = "Informe o código do sensor.")]
    [StringLength(40, ErrorMessage = "O código do sensor pode ter até 40 caracteres.")]
    string Sensor,
    [Range(0, 100000, ErrorMessage = "O valor precisa estar entre 0 e 100000.")]
    double Valor);

public record NovoRelatoRequest(
    [Range(1, int.MaxValue, ErrorMessage = "Informe o equipamento.")]
    int EquipamentoId,
    [Required(ErrorMessage = "Descreva o que aconteceu.")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "A descrição precisa ter de 5 a 500 caracteres.")]
    string Descricao);

public record EditarRelatoRequest(
    [Required(ErrorMessage = "Descreva o que aconteceu.")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "A descrição precisa ter de 5 a 500 caracteres.")]
    string Descricao);

public record StatusRelatoRequest(
    [Required(ErrorMessage = "Informe a situação do relato.")]
    StatusRelato? Status);
