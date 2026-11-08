namespace Sobe.Api.Models;

public enum Perfil { Admin, Cidadao }

public class Usuario
{
    public int Id { get; set; }
    public required string Nome { get; set; }
    public required string Email { get; set; }
    public required string SenhaHash { get; set; }
    public Perfil Perfil { get; set; } = Perfil.Cidadao;
    public DateTime CriadoEm { get; set; }
}
