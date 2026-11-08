namespace Sobe.Api.Models;

public enum StatusRelato { Aberto, Confirmado, Resolvido, Descartado }

public class Relato
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public int EquipamentoId { get; set; }
    public Equipamento Equipamento { get; set; } = null!;
    public required string Descricao { get; set; }
    public StatusRelato Status { get; set; } = StatusRelato.Aberto;
    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }
}
