using Microsoft.EntityFrameworkCore;
using Agenda.Data;
using Agenda.Domain;

namespace Agenda.Repositories;

public class UsuarioRepository : IUsuarioRepository {
    private readonly TicketContext context;

    public UsuarioRepository(TicketContext context) {
        this.context = context;
    }

    public async Task<Usuario?> ObtenerPorIdAsync(int id) {
        return await this.context.Usuarios.FindAsync(id);
    }

    public async Task<Usuario?> ObtenerPorEmailAsync(string email) {
        return await this.context.Usuarios.FirstOrDefaultAsync(usuario => usuario.Email == email);
    }

    public async Task<Usuario?> ObtenerPorTokenAsync(string token) {
        return await this.context.Usuarios.FirstOrDefaultAsync(usuario => usuario.Token == token);
    }

    public async Task<IEnumerable<Usuario>> ObtenerTodosAsync() {
        return await this.context.Usuarios.ToListAsync();
    }

    public async Task AgregarAsync(Usuario usuario) {
        await this.context.Usuarios.AddAsync(usuario);
    }

    public async Task GuardarCambiosAsync() {
        await this.context.SaveChangesAsync();
    }
}