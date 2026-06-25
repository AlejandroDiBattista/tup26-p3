using tp5.Models;
namespace tp5.Servicios;
public interface IContactoService
{
    Task<List<Contacto>> ListarAsync(string? filtro = null);
    Task<Contacto?> ObtenerPorIdAsync(int id);
    Task<Contacto> CrearAsync(Contacto nuevo);
    Task<bool> ActualizarAsync(Contacto modificado);
    Task<bool> EliminarAsync(int id);
}