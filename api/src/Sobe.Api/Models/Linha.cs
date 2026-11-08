namespace Sobe.Api.Models;

public class Linha
{
    public int Id { get; set; }
    public int Numero { get; set; }
    public required string Nome { get; set; }
    public required string Cor { get; set; }
    public required string Operadora { get; set; }
    public List<Estacao> Estacoes { get; set; } = [];
}
