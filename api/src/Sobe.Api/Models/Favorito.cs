namespace Sobe.Api.Models;

public class Favorito
{
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public int EstacaoId { get; set; }
    public Estacao Estacao { get; set; } = null!;
    public DateTime CriadoEm { get; set; }
}
