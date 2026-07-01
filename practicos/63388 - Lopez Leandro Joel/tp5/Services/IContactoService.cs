using tp5.Models;

namespace tp5.Services;

public interface IContactoService
{
    Task<List<Contacto>> GetTodosAsync(string? busqueda = null);
    Task<Contacto?> GetPorIdAsync(int id);
    Task<Contacto> CrearAsync(Contacto contacto);
    Task<Contacto> ActualizarAsync(Contacto contacto);
    Task<bool> EliminarAsync(int id);
}
