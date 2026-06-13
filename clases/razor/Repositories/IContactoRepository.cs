using razor.Models;

namespace razor.Repositories;

public interface IContactoRepository {
    Task<List<Contacto>> ObtenerTodosAsync();
    Task<Contacto?> ObtenerPorIdAsync(int id);
    Task CrearAsync(Contacto contacto);
    Task ActualizarAsync(Contacto contacto);
    Task EliminarAsync(int id);
}
