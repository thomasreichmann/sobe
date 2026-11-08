namespace Sobe.Api.Models;

public enum TipoSensor { Movimento, Porta }

public class Sensor
{
    public int Id { get; set; }
    public int EquipamentoId { get; set; }
    public Equipamento Equipamento { get; set; } = null!;
    public TipoSensor Tipo { get; set; }
    public required string Codigo { get; set; }
    public double Limite { get; set; }
    public required string Unidade { get; set; }
    public bool Ativo { get; set; } = true;

    public bool Ultrapassa(double valor) => valor > Limite;

    public string DescreverParada(double valor)
    {
        if (Tipo == TipoSensor.Porta) return $"Porta aberta há {valor:0} s (limite de {Limite:0} s)";
        return $"Sem movimento há {valor:0} min (limite de {Limite:0} min)";
    }

    public static Sensor Padrao(TipoSensor tipo, int equipamentoId)
    {
        var porta = tipo == TipoSensor.Porta;
        return new Sensor
        {
            EquipamentoId = equipamentoId,
            Tipo = tipo,
            Codigo = $"EQ{equipamentoId:000}-{(porta ? "PORTA" : "MOV")}",
            Limite = porta ? 120 : 15,
            Unidade = porta ? "s" : "min",
        };
    }
}

public class Leitura
{
    public long Id { get; set; }
    public int SensorId { get; set; }
    public Sensor Sensor { get; set; } = null!;
    public double Valor { get; set; }
    public DateTime RegistradaEm { get; set; }
}
