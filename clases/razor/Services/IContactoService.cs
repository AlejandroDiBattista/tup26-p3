using razor.Models;

namespace razor.Services;

public interface IContactoService {
    Task<List<Contacto>> ListarAsync();
    Task<Contacto?> BuscarAsync(int id);
    Task CrearAsync(string nombre, string apellido, string telefono, string? email, bool favorito);
    Task ActualizarAsync(int id, string nombre, string apellido, string telefono, string? email, bool favorito);
    Task EliminarAsync(int id);
}
