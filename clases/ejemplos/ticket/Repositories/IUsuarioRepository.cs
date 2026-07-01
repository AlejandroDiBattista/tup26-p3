using Agenda.Domain;

namespace Agenda.Repositories;

public interface IUsuarioRepository {
    Task<Usuario?> ObtenerPorIdAsync(int id);
    Task<Usuario?> ObtenerPorEmailAsync(string email);
    Task<Usuario?> ObtenerPorTokenAsync(string token);
    Task<IEnumerable<Usuario>> ObtenerTodosAsync();
    Task AgregarAsync(Usuario usuario);
    Task GuardarCambiosAsync();
}