namespace Sobe.Api.Models;

public class Alerta
{
    public int Id { get; set; }
    public int EquipamentoId { get; set; }
    public Equipamento Equipamento { get; set; } = null!;
    public int? SensorId { get; set; }
    public Sensor? Sensor { get; set; }
    public required string Motivo { get; set; }
    public DateTime AbertoEm { get; set; }
    public DateTime? EncerradoEm { get; set; }

    public bool Aberto => EncerradoEm is null;

    public double MinutosParado(DateTime agora) => ((EncerradoEm ?? agora) - AbertoEm).TotalMinutes;
}
