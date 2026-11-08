using Microsoft.EntityFrameworkCore;
using Sobe.Api.Data;
using Sobe.Api.Models;

namespace Sobe.Api.Repositories;

public interface IUsuarioRepository
{
    Task<Usuario?> BuscarAsync(int id);
    Task<Usuario?> BuscarPorEmailAsync(string email);
    void Adicionar(Usuario usuario);
    Task SalvarAsync();
}

public class UsuarioRepository(SobeDbContext db) : IUsuarioRepository
{
    public Task<Usuario?> BuscarAsync(int id) => db.Usuarios.FirstOrDefaultAsync(u => u.Id == id);

    public Task<Usuario?> BuscarPorEmailAsync(string email) => db.Usuarios.FirstOrDefaultAsync(u => u.Email == email);

    public void Adicionar(Usuario usuario) => db.Usuarios.Add(usuario);

    public Task SalvarAsync() => db.SaveChangesAsync();
}
