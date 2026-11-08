namespace Sobe.Api.Models;

public enum TipoEquipamento { Elevador, EscadaRolante, Plataforma }

public enum StatusEquipamento { Operando, Parado, Manutencao }

public class Equipamento
{
    public int Id { get; set; }
    public int EstacaoId { get; set; }
    public Estacao Estacao { get; set; } = null!;
    public TipoEquipamento Tipo { get; set; }
    public required string Localizacao { get; set; }
    public StatusEquipamento Status { get; set; } = StatusEquipamento.Operando;
    public DateTime StatusDesde { get; set; }
    public List<Sensor> Sensores { get; set; } = [];
    public List<Alerta> Alertas { get; set; } = [];

    public void MudarStatus(StatusEquipamento novo, DateTime quando)
    {
        if (Status == novo) return;
        Status = novo;
        StatusDesde = quando;
    }
}
