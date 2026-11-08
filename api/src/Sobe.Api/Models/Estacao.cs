namespace Sobe.Api.Models;

public class Estacao
{
    public int Id { get; set; }
    public int LinhaId { get; set; }
    public Linha Linha { get; set; } = null!;
    public required string Nome { get; set; }
    public int Ordem { get; set; }
    public List<Equipamento> Equipamentos { get; set; } = [];

    public int ForaDeServico => Equipamentos.Count(e => e.Status != StatusEquipamento.Operando);

    // Escada rolante não dá acesso a quem usa cadeira de rodas, então não entra na conta.
    public bool Acessivel
    {
        get
        {
            var essenciais = Equipamentos.Where(e => e.Tipo != TipoEquipamento.EscadaRolante).ToList();
            return essenciais.Count > 0 && essenciais.All(e => e.Status == StatusEquipamento.Operando);
        }
    }
}
